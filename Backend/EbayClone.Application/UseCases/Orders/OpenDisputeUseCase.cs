using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IOpenDisputeUseCase
    {
        Task<Guid> ExecuteAsync(OpenDisputeRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5C Bước 1: Buyer mock mở Dispute/Case.
    /// Rules: INR chỉ mở sau 30 ngày delivery. SNAD mở bất cứ lúc nào (DELIVERED).
    /// </summary>
    public class OpenDisputeUseCase : IOpenDisputeUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderDisputeRepository _disputeRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OpenDisputeUseCase(
            IOrderRepository orderRepository,
            IOrderDisputeRepository disputeRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _disputeRepository = disputeRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(OpenDisputeRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    throw new ArgumentException("Order not found.");

                // [Validation] Chỉ DELIVERED hoặc RETURN_REQUESTED (buyer escalate decline)
                if (order.Status != "DELIVERED" && order.Status != "RETURN_REQUESTED")
                    throw new InvalidOperationException($"Không thể mở dispute ở trạng thái '{order.Status}'.");

                // [Validation] Type phải hợp lệ
                if (request.Type != "INR" && request.Type != "SNAD")
                    throw new ArgumentException("Dispute type phải là 'INR' hoặc 'SNAD'.");

                // [Validation] Không cho phép mở nhiều dispute cùng lúc
                var existing = await _disputeRepository.GetActiveByOrderIdAsync(order.Id, cancellationToken);
                if (existing != null)
                    throw new InvalidOperationException("Đơn hàng đã có dispute đang xử lý.");

                var dispute = new OrderDispute
                {
                    OrderId = order.Id,
                    BuyerId = order.BuyerId,
                    Type = request.Type,
                    BuyerMessage = request.BuyerMessage,
                    BuyerEvidenceUrls = request.BuyerEvidenceUrls
                };
                dispute.InitializeDeadline(); // +3 ngày seller phải respond

                // Order → DISPUTE_OPENED
                order.MarkAsDisputeOpened();

                // [Tài chính] Hold tiền đơn này — chuyển từ Pending/Available → OnHold
                // Seller không thể rút tiền đơn này cho đến khi dispute resolve
                var wallet = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                if (wallet != null)
                {
                    wallet.HoldForDispute(order.TotalAmount);
                    _walletRepository.Update(wallet);

                    await _disputeRepository.AddAsync(dispute, cancellationToken);

                    // Ghi WalletTransaction DISPUTE_HOLD
                    // Amount = -TotalAmount: tiền vẫn trong ví (chuyển sang OnHold),
                    // âm để mô tả "tiền rời khỏi lưu thông". BalanceAfter = TotalBalance (không đổi).
                    await _walletTransactionRepository.AddAsync(new WalletTransaction
                    {
                        ShopId = order.ShopId,
                        Amount = -order.TotalAmount,
                        Type = "DISPUTE_HOLD",
                        ReferenceId = order.Id,
                        ReferenceType = "ORDER_DISPUTE",
                        OrderNumber = order.OrderNumber,
                        Description = $"Tạm giữ {order.TotalAmount:N0} đ sang OnHold cho dispute — Đơn #{order.OrderNumber} (tiền không mất, chỉ lock)",
                        BalanceAfter = wallet.TotalBalance
                    }, cancellationToken);
                }
                else
                {
                    await _disputeRepository.AddAsync(dispute, cancellationToken);
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return dispute.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
