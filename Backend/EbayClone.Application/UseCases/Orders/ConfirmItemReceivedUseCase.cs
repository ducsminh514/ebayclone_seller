using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IConfirmItemReceivedUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid returnId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A Bước 3 (ACCEPT_RETURN path): Seller confirm đã nhận lại hàng từ buyer.
    /// Trigger: Sau khi Order.Status = "RETURN_IN_PROGRESS" và seller nhận được item.
    ///
    /// Wallet flow:
    ///   wallet.ProcessRefund(TotalAmount) → trừ OnHold/Pending/Available
    ///   WalletTx: REFUND -TotalAmount
    ///   order.MarkAsRefunded()
    ///   return.MarkRefunded(TotalAmount, 0)
    /// </summary>
    public class ConfirmItemReceivedUseCase : IConfirmItemReceivedUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IProductRepository _productRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConfirmItemReceivedUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IProductRepository productRepository,
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository  = orderRepository;
            _returnRepository = returnRepository;
            _walletRepository = walletRepository;
            _txRepository     = txRepository;
            _productRepository = productRepository;
            _shopRepository   = shopRepository;
            _unitOfWork       = unitOfWork;
        }


        public async Task ExecuteAsync(Guid shopId, Guid returnId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnEntity = await _returnRepository.GetByIdAsync(returnId, cancellationToken);
                if (returnEntity == null)
                    throw new ArgumentException("Return request not found.");

                var order = returnEntity.Order;
                if (order == null)
                    throw new InvalidOperationException("Order associated with return not found.");

                // [Security] Chỉ seller của đơn mới được confirm
                if (order.ShopId != shopId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xử lý return request này.");

                // [Validation] Phải đang ở trạng thái RETURN_IN_PROGRESS (seller đã ACCEPT_RETURN)
                if (order.Status != "RETURN_IN_PROGRESS")
                    throw new InvalidOperationException(
                        $"Đơn hàng đang ở trạng thái '{order.Status}'. Chỉ confirm khi RETURN_IN_PROGRESS.");

                if (returnEntity.Status != "ACCEPTED")
                    throw new InvalidOperationException("Return request phải ở trạng thái ACCEPTED.");

                // Cập nhật return entity
                returnEntity.MarkRefunded(order.TotalAmount, 0);

                // Cập nhật order
                order.MarkAsRefunded();

                // Xử lý ví seller — hoàn tiền
                var wallet = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                if (wallet != null)
                {
                    var (fromOnHold, fromPending, fromAvailable) = wallet.ProcessRefund(order.TotalAmount);
                    _walletRepository.Update(wallet);

                    var sources = new System.Collections.Generic.List<string>();
                    if (fromOnHold > 0)    sources.Add($"Hold: -{fromOnHold:N0}");
                    if (fromPending > 0)   sources.Add($"Pending: -{fromPending:N0}");
                    if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                    var breakdown = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                    await _txRepository.AddAsync(new WalletTransaction
                    {
                        ShopId        = shopId,
                        Amount        = -order.TotalAmount,
                        Type          = "REFUND",
                        ReferenceId   = order.Id,
                        ReferenceType = "ORDER_RETURN",
                        OrderNumber   = order.OrderNumber,
                        Description   = $"Hoàn tiền full sau nhận hàng trả lại — Đơn #{order.OrderNumber}{breakdown}",
                        BalanceAfter  = wallet.TotalBalance
                    }, cancellationToken);
                }

                _returnRepository.Update(returnEntity);
                _orderRepository.Update(order);

                // B4 Fix: Hoàn kho sau khi nhận hàng về — chỉ hoàn 1 lần
                if (!returnEntity.IsStockRestored)
                {
                    returnEntity.IsStockRestored = true;
                    var restoredProductIds = new System.Collections.Generic.HashSet<Guid>();
                    int activeListingDelta = 0;
                    foreach (var item in order.Items)
                    {
                        await _productRepository.RestoreStockAtomicAsync(
                            item.VariantId, item.Quantity, cancellationToken);

                        // Cập nhật status sản phẩm: nếu đang OUT_OF_STOCK → về ACTIVE
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

                    // [FIX-S1] Apply activeListingDelta to shop
                    if (activeListingDelta != 0)
                    {
                        var shopForListing = await _shopRepository.GetByIdAsync(order.ShopId, cancellationToken);
                        if (shopForListing != null)
                        {
                            shopForListing.ActiveListingCount = Math.Max(0, shopForListing.ActiveListingCount + activeListingDelta);
                            _shopRepository.Update(shopForListing);
                        }
                    }
                }

                // [FIX-W2c] Giảm TotalTransactions + TotalSalesAmount khi return full refund
                var shopReturn = await _shopRepository.GetByIdAsync(order.ShopId, cancellationToken);
                if (shopReturn != null)
                {
                    shopReturn.TotalTransactions = Math.Max(0, shopReturn.TotalTransactions - 1);
                    shopReturn.TotalSalesAmount = Math.Max(0, shopReturn.TotalSalesAmount - order.ItemSubtotal);
                    _shopRepository.Update(shopReturn);
                }

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
