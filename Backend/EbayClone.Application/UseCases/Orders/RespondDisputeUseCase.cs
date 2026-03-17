using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IRespondDisputeUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid disputeId, RespondDisputeRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5C Bước 2: Seller respond to dispute — nộp bằng chứng (tracking, ảnh).
    /// </summary>
    public class RespondDisputeUseCase : IRespondDisputeUseCase
    {
        private readonly IOrderDisputeRepository _disputeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RespondDisputeUseCase(
            IOrderDisputeRepository disputeRepository,
            IUnitOfWork unitOfWork)
        {
            _disputeRepository = disputeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid disputeId, RespondDisputeRequest request, CancellationToken cancellationToken = default)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId, cancellationToken);
            if (dispute == null)
                throw new ArgumentException("Dispute not found.");

            if (dispute.Order == null || dispute.Order.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền xử lý dispute này.");

            // [Concurrency]
            if (dispute.RowVersion != null && request.RowVersion != null
                && !request.RowVersion.SequenceEqual(dispute.RowVersion))
            {
                throw new InvalidOperationException("Dispute đã được cập nhật bởi phiên khác.");
            }

            if (string.IsNullOrWhiteSpace(request.SellerMessage))
                throw new ArgumentException("Seller phải gửi tin nhắn phản hồi.");

            dispute.SellerRespond(request.SellerMessage, request.SellerEvidenceUrls);
            _disputeRepository.Update(dispute);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
