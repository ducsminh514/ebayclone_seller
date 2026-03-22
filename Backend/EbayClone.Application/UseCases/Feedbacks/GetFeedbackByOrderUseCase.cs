using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Shared.DTOs.Feedbacks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Feedbacks
{
    public interface IGetFeedbackByOrderUseCase
    {
        Task<FeedbackDto?> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default);
    }

    public class GetFeedbackByOrderUseCase : IGetFeedbackByOrderUseCase
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public GetFeedbackByOrderUseCase(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        public async Task<FeedbackDto?> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var feedback = await _feedbackRepository.GetByOrderIdAsync(orderId, cancellationToken);
            if (feedback == null) return null;

            return new FeedbackDto
            {
                Id = feedback.Id,
                OrderId = feedback.OrderId,
                BuyerId = feedback.BuyerId,
                ShopId = feedback.ShopId,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                BuyerName = MaskBuyerName(feedback.Buyer?.Username ?? "Buyer"),
                OrderNumber = feedback.Order?.OrderNumber,
                OrderAmount = feedback.Order?.TotalAmount ?? 0,
                SellerReply = feedback.SellerReply,
                SellerRepliedAt = feedback.SellerRepliedAt,
                CreatedAt = feedback.CreatedAt
            };
        }

        private string MaskBuyerName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 2) return "***";
            return $"{name[0]}***{name[^1]}";
        }
    }
}
