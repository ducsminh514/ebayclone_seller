using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Shared.DTOs.Feedbacks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Feedbacks
{
    public interface IReplyFeedbackUseCase
    {
        Task<FeedbackDto> ExecuteAsync(Guid shopId, Guid feedbackId, ReplyFeedbackRequest request, CancellationToken cancellationToken = default);
    }

    public class ReplyFeedbackUseCase : IReplyFeedbackUseCase
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReplyFeedbackUseCase(IFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork)
        {
            _feedbackRepository = feedbackRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<FeedbackDto> ExecuteAsync(Guid shopId, Guid feedbackId, ReplyFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(feedbackId, cancellationToken)
                ?? throw new InvalidOperationException($"Feedback {feedbackId} không tồn tại.");

            // Security: chỉ seller của shop này mới được reply
            if (feedback.ShopId != shopId)
                throw new InvalidOperationException("Bạn không có quyền reply feedback này.");

            // Domain logic: chỉ reply 1 lần (validate bên trong entity)
            feedback.SetSellerReply(request.Reply);

            _feedbackRepository.Update(feedback);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new FeedbackDto
            {
                Id = feedback.Id,
                OrderId = feedback.OrderId,
                BuyerId = feedback.BuyerId,
                ShopId = feedback.ShopId,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                BuyerName = feedback.Buyer?.Username ?? feedback.Buyer?.FullName ?? "Buyer",
                OrderNumber = feedback.Order?.OrderNumber,
                OrderAmount = feedback.Order?.TotalAmount ?? 0,
                SellerReply = feedback.SellerReply,
                SellerRepliedAt = feedback.SellerRepliedAt,
                CreatedAt = feedback.CreatedAt
            };
        }
    }
}
