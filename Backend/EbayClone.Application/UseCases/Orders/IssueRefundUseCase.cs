using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IIssueRefundUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid returnId, IssueRefundRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A Bước 3: Seller nhận hàng + quyết định refund + stock.
    /// Buyer đã ship hàng lại (IN_PROGRESS) → seller inspect → refund.
    /// </summary>
    public class IssueRefundUseCase : IIssueRefundUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public IssueRefundUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _returnRepository = returnRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid returnId, IssueRefundRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnEntity = await _returnRepository.GetByIdAsync(returnId, cancellationToken);
                if (returnEntity == null)
                    throw new ArgumentException("Return request not found.");

                var order = returnEntity.Order;
                if (order == null || order.ShopId != shopId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xử lý return này.");

                // [Concurrency] Check RowVersion
                if (returnEntity.RowVersion != null && request.RowVersion != null 
                    && !request.RowVersion.SequenceEqual(returnEntity.RowVersion))
                {
                    throw new InvalidOperationException("Return đã được cập nhật bởi phiên khác. Vui lòng tải lại.");
                }

                // [Validation] Chỉ IN_PROGRESS (buyer đã gửi hàng lại) mới được refund
                if (returnEntity.Status != "IN_PROGRESS")
                    throw new InvalidOperationException($"Return phải ở trạng thái IN_PROGRESS để issue refund (hiện: {returnEntity.Status}).");

                // [Validation] Deduction max 50% (eBay rule: Free Returns)
                if (request.DeductionAmount > order.TotalAmount * 0.5m)
                    throw new InvalidOperationException("Deduction tối đa 50% giá trị đơn hàng (eBay Free Returns policy).");

                if (request.RefundAmount + request.DeductionAmount > order.TotalAmount)
                    throw new InvalidOperationException("Tổng refund + deduction không được vượt giá trị đơn hàng.");

                // Mark nhận hàng
                returnEntity.MarkReturnReceived();

                // Issue refund
                returnEntity.MarkRefunded(request.RefundAmount, request.DeductionAmount);
                returnEntity.DeductionReason = request.DeductionReason;

                // Cập nhật Order status
                if (request.DeductionAmount > 0)
                    order.MarkAsPartiallyRefunded();
                else
                    order.MarkAsRefunded();

                // Hoàn ví
                var wallet = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                if (wallet != null)
                {
                    var (fromOnHold, fromPending, fromAvailable) = wallet.ProcessRefund(request.RefundAmount);
                    _walletRepository.Update(wallet);

                    var sources = new System.Collections.Generic.List<string>();
                    if (fromOnHold > 0) sources.Add($"Hold: -{fromOnHold:N0}");
                    if (fromPending > 0) sources.Add($"Pending: -{fromPending:N0}");
                    if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                    var balanceNote = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                    await _txRepository.AddAsync(new Domain.Entities.WalletTransaction
                    {
                        ShopId = shopId,
                        Amount = -request.RefundAmount,
                        Type = "REFUND",
                        ReferenceId = order.Id,
                        ReferenceType = "ORDER_RETURN",
                        Description = $"Hoàn tiền {request.RefundAmount:N0} đ (Return) — Đơn #{order.OrderNumber}{balanceNote}",
                        BalanceAfter = wallet.PendingBalance + wallet.AvailableBalance + wallet.OnHoldBalance
                    }, cancellationToken);
                }

                // Restore stock nếu seller quyết định
                if (request.RestoreStock)
                {
                    returnEntity.IsStockRestored = true;
                    foreach (var item in order.Items)
                    {
                        await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                    }
                }

                _returnRepository.Update(returnEntity);
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
