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
        private readonly IUnitOfWork _unitOfWork;

        public ResolveDisputeUseCase(
            IOrderRepository orderRepository,
            IOrderDisputeRepository disputeRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _disputeRepository = disputeRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
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
                                Description = $"Hoàn tiền dispute (Buyer win) — Đơn #{order.OrderNumber}. Defect +1.{balanceNote}",
                                BalanceAfter = walletRefund.PendingBalance + walletRefund.AvailableBalance + walletRefund.OnHoldBalance
                            }, cancellationToken);
                        }
                        break;

                    case "SELLER_WIN":
                        dispute.ResolveSellerWin(); // IsDefect = false
                        
                        // Release funds → COMPLETED
                        var walletRelease = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                        if (walletRelease != null)
                        {
                            decimal profit = order.TotalAmount - order.PlatformFee;
                            walletRelease.ProcessRelease(order.TotalAmount, profit);
                            _walletRepository.Update(walletRelease);

                            await _txRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = order.ShopId,
                                Amount = profit,
                                Type = "ESCROW_RELEASE",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER_DISPUTE",
                                Description = $"Giải ngân dispute (Seller win) — Đơn #{order.OrderNumber}. Thực nhận: {profit:N0} đ.",
                                BalanceAfter = walletRelease.TotalBalance
                            }, cancellationToken);
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
