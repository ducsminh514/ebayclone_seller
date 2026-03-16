using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IRespondReturnUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid returnId, RespondReturnRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A Bước 2: Seller respond return request.
    /// 4 options: ACCEPT_RETURN, PARTIAL_REFUND, FULL_REFUND_KEEP_ITEM, DECLINE
    /// </summary>
    public class RespondReturnUseCase : IRespondReturnUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RespondReturnUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _returnRepository = returnRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid returnId, RespondReturnRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnEntity = await _returnRepository.GetByIdAsync(returnId, cancellationToken);
                if (returnEntity == null)
                    throw new ArgumentException("Return request not found.");

                var order = returnEntity.Order;
                if (order == null || order.ShopId != shopId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xử lý return request này.");

                // [Concurrency] Check RowVersion
                if (returnEntity.RowVersion != null && request.RowVersion != null 
                    && !request.RowVersion.SequenceEqual(returnEntity.RowVersion))
                {
                    throw new InvalidOperationException("Return request đã được cập nhật bởi phiên khác. Vui lòng tải lại.");
                }

                switch (request.ResponseType)
                {
                    case "ACCEPT_RETURN":
                        // Seller chấp nhận → buyer sẽ gửi hàng lại
                        returnEntity.AcceptReturn("ACCEPT_RETURN", request.SellerMessage);
                        order.MarkAsReturnInProgress();
                        break;

                    case "FULL_REFUND_KEEP_ITEM":
                        // Seller hoàn full + buyer giữ hàng → hoàn ví ngay
                        returnEntity.AcceptReturn("FULL_REFUND_KEEP_ITEM", request.SellerMessage);
                        returnEntity.MarkRefunded(order.TotalAmount, 0);
                        order.MarkAsRefunded();

                        // Hoàn PendingBalance
                        await DeductPendingAsync(shopId, order, "Hoàn tiền full (buyer giữ hàng)", cancellationToken);
                        break;

                    case "PARTIAL_REFUND":
                        // Seller offer partial → cần buyer accept (mock: auto-accept)
                        if (!request.PartialRefundAmount.HasValue || request.PartialRefundAmount.Value <= 0)
                            throw new ArgumentException("Số tiền hoàn 1 phần phải > 0.");
                        if (request.PartialRefundAmount.Value > order.TotalAmount)
                            throw new ArgumentException("Số tiền hoàn không được vượt quá tổng đơn hàng.");

                        var deduction = order.TotalAmount - request.PartialRefundAmount.Value;
                        returnEntity.AcceptReturn("PARTIAL_REFUND", request.SellerMessage);
                        returnEntity.MarkRefunded(request.PartialRefundAmount.Value, deduction);
                        order.MarkAsPartiallyRefunded();

                        // Hoàn partial vào ví
                        await DeductPendingAsync(shopId, order, 
                            $"Hoàn tiền 1 phần {request.PartialRefundAmount.Value:N0} đ", 
                            cancellationToken, request.PartialRefundAmount.Value);
                        break;

                    case "DECLINE":
                        // [eBay Rule] SNAD/Damaged → KHÔNG được decline (Money Back Guarantee)
                        if (returnEntity.Reason == "NOT_AS_DESCRIBED" || returnEntity.Reason == "DAMAGED")
                            throw new InvalidOperationException("Không thể từ chối return SNAD/Damaged (Money Back Guarantee).");

                        // Chỉ decline nếu: No Returns policy + buyer đổi ý
                        returnEntity.DeclineReturn(request.SellerMessage);
                        // Order giữ RETURN_REQUESTED → buyer có thể escalate (GĐ5C)
                        break;

                    default:
                        throw new ArgumentException($"ResponseType không hợp lệ: '{request.ResponseType}'.");
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

        private async Task DeductPendingAsync(Guid shopId, Domain.Entities.Order order, string desc, CancellationToken ct, decimal? customAmount = null)
        {
            var wallet = await _walletRepository.GetByShopIdAsync(shopId, ct);
            if (wallet != null)
            {
                var amount = customAmount ?? order.TotalAmount;
                wallet.DeductPending(amount);
                _walletRepository.Update(wallet);

                await _txRepository.AddAsync(new Domain.Entities.WalletTransaction
                {
                    ShopId = shopId,
                    Amount = -amount,
                    Type = "REFUND",
                    ReferenceId = order.Id,
                    ReferenceType = "ORDER_RETURN",
                    Description = $"{desc} — Đơn #{order.OrderNumber}",
                    BalanceAfter = wallet.PendingBalance
                }, ct);
            }
        }
    }
}
