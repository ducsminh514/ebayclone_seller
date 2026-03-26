using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IResolveDisputeUseCase
    {
        Task ExecuteAsync(Guid disputeId, ResolveDisputeRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5C Bước 3: Platform resolve dispute — buyer win hoặc seller win.
    /// Buyer win → REFUNDED + defect. Seller win → COMPLETED + funds release.
    /// </summary>
    public class ResolveDisputeUseCase : IResolveDisputeUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderDisputeRepository _disputeRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IProductRepository _productRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ResolveDisputeUseCase(
            IOrderRepository orderRepository,
            IOrderDisputeRepository disputeRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IProductRepository productRepository,
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _disputeRepository = disputeRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid disputeId, ResolveDisputeRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var dispute = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
                if (dispute == null)
                    throw new ArgumentException("Dispute not found.");

                var order = dispute.Order;
                if (order == null)
                    throw new InvalidOperationException("Order associated with dispute not found.");

                switch (request.Resolution)
                {
                    case "BUYER_WIN":
                        dispute.ResolveBuyerWin(); // IsDefect = true
                        order.MarkAsRefunded();

                        // Hoàn PendingBalance cho buyer
                        var walletRefund = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                        if (walletRefund != null)
                        {
                            var (fromOnHold, fromPending, fromAvailable) = walletRefund.ProcessRefund(order.TotalAmount);
                            _walletRepository.Update(walletRefund);

                            var sources = new System.Collections.Generic.List<string>();
                            if (fromOnHold > 0) sources.Add($"Hold: -{fromOnHold:N0}");
                            if (fromPending > 0) sources.Add($"Pending: -{fromPending:N0}");
                            if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                            var balanceNote = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                            await _txRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = order.ShopId,
                                Amount = -order.TotalAmount,
                                Type = "REFUND",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER_DISPUTE",
                                OrderNumber = order.OrderNumber,
                                Description = $"Hoàn tiền dispute (Buyer win) — Đơn #{order.OrderNumber}. Defect +1.{balanceNote}",
                                BalanceAfter = walletRefund.PendingBalance + walletRefund.AvailableBalance + walletRefund.OnHoldBalance
                            }, cancellationToken);
                        }

                        // [FIX-F4] Hoàn kho khi Buyer Win — tránh inventory leak
                        var restoredProductIds = new System.Collections.Generic.HashSet<Guid>();
                        int activeListingDelta = 0;
                        foreach (var item in order.Items)
                        {
                            await _productRepository.RestoreStockAtomicAsync(
                                item.VariantId, item.Quantity, cancellationToken);

                            var variant = await _productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken);
                            if (variant != null && restoredProductIds.Add(variant.ProductId))
                            {
                                var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
                                if (product != null)
                                {
                                    var oldStatus = product.Status;
                                    product.CheckAndUpdateStockStatus();
                                    // [FIX-S1] Track ACTIVE↔OUT_OF_STOCK transitions
                                    if (oldStatus != product.Status)
                                    {
                                        if (product.Status == "ACTIVE") activeListingDelta++;
                                        else if (oldStatus == "ACTIVE") activeListingDelta--;
                                    }
                                    await _productRepository.UpdateAsync(product, cancellationToken);
                                }
                            }
                        }

                        // [FIX-W2b] Giảm TotalTransactions + TotalSalesAmount khi BuyerWin (full refund)
                        var shopDispute = await _shopRepository.GetByIdAsync(order.ShopId, cancellationToken);
                        if (shopDispute != null)
                        {
                            shopDispute.TotalTransactions = Math.Max(0, shopDispute.TotalTransactions - 1);
                            shopDispute.TotalSalesAmount = Math.Max(0, shopDispute.TotalSalesAmount - order.ItemSubtotal);
                            // [FIX-S1] Sync ActiveListingCount
                            if (activeListingDelta != 0)
                            {
                                shopDispute.ActiveListingCount = Math.Max(0, shopDispute.ActiveListingCount + activeListingDelta);
                            }
                            _shopRepository.Update(shopDispute);
                        }
                        break;

                    case "SELLER_WIN":
                        dispute.ResolveSellerWin(); // IsDefect = false
                        
                        // Release funds từ OnHold → Available (tiền đang lock ở OnHold do HoldForDispute)
                        // KHÔNG dùng ProcessRelease (từ Pending) — sẽ sai nguồn tiền
                        var walletRelease = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                        if (walletRelease != null)
                        {
                            decimal profit = order.TotalAmount - order.PlatformFee;
                            var (actualDebit, actualCredit) = walletRelease.ProcessReleaseFromHold(order.TotalAmount, profit);
                            _walletRepository.Update(walletRelease);

                            if (actualDebit > 0)
                            {
                                decimal actualFee = actualDebit - actualCredit;

                                // Log 1: Phí sàn
                                await _txRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = order.ShopId,
                                    Amount = -actualFee,
                                    Type = "PLATFORM_FEE",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER_DISPUTE",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Phí sàn 5% — Đơn #{order.OrderNumber} (Dispute Seller Win, thực thu: {actualFee:N0} đ)",
                                    BalanceAfter = walletRelease.TotalBalance
                                }, cancellationToken);

                                // Log 2: Giải ngân
                                await _txRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = order.ShopId,
                                    Amount = actualCredit,
                                    Type = "ESCROW_RELEASE",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER_DISPUTE",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Giải ngân dispute (Seller win) — Đơn #{order.OrderNumber}. Thực nhận: {actualCredit:N0} đ (phí: {actualFee:N0} đ).",
                                    BalanceAfter = walletRelease.TotalBalance
                                }, cancellationToken);
                            }
                            // actualDebit=0: OnHold cạn (edge case) — không log, tiền đã xử lý nơi khác
                        }

                        order.MarkAsCompleted();
                        break;

                    default:
                        throw new ArgumentException($"Resolution không hợp lệ: '{request.Resolution}'. Phải là 'BUYER_WIN' hoặc 'SELLER_WIN'.");
                }

                _disputeRepository.Update(dispute);
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
