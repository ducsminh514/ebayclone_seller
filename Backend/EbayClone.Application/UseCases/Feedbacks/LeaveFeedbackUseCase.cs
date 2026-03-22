using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Feedbacks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Feedbacks
{
    public interface ILeaveFeedbackUseCase
    {
        Task<FeedbackDto> ExecuteAsync(Guid buyerId, LeaveFeedbackRequest request, CancellationToken cancellationToken = default);
    }

    public class LeaveFeedbackUseCase : ILeaveFeedbackUseCase
    {
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LeaveFeedbackUseCase(
            IFeedbackRepository feedbackRepository,
            IOrderRepository orderRepository,
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _feedbackRepository = feedbackRepository;
            _orderRepository = orderRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<FeedbackDto> ExecuteAsync(Guid buyerId, LeaveFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Validate order
            var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
                ?? throw new InvalidOperationException($"Order {request.OrderId} không tồn tại.");

            // 2. Validate buyer owns this order
            if (order.BuyerId != buyerId)
                throw new InvalidOperationException("Bạn không phải buyer của đơn hàng này.");

            // 3. Validate order status
            if (order.Status != "DELIVERED" && order.Status != "COMPLETED")
                throw new InvalidOperationException($"Chỉ có thể để lại feedback khi đơn hàng đã DELIVERED hoặc COMPLETED. Status hiện tại: {order.Status}.");

            // 4. Validate chưa có feedback cho order này
            var existing = await _feedbackRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException("Đơn hàng này đã có feedback rồi. Mỗi đơn chỉ được 1 feedback.");

            // 5. Validate thời hạn 60 ngày
            if (order.DeliveredAt.HasValue && order.DeliveredAt.Value.AddDays(60) < DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Đã quá hạn 60 ngày để để lại feedback.");

            // 6. Tạo feedback (domain validation bên trong)
            var feedback = Feedback.Create(
                request.OrderId, buyerId, order.ShopId,
                request.Rating, request.Comment);

            await _feedbackRepository.AddAsync(feedback, cancellationToken);

            // 7. Cập nhật Shop feedback stats (denormalized)
            var shop = await _shopRepository.GetByIdAsync(order.ShopId, cancellationToken);
            if (shop != null)
            {
                var (positive, neutral, negative) = await _feedbackRepository
                    .GetShopFeedbackCountsAsync(order.ShopId, cancellationToken);

                // Cộng thêm feedback mới (chưa save nên chưa count)
                switch (request.Rating)
                {
                    case FeedbackRatings.POSITIVE: positive++; break;
                    case FeedbackRatings.NEUTRAL: neutral++; break;
                    case FeedbackRatings.NEGATIVE: negative++; break;
                }

                shop.UpdateFeedbackStats(positive, neutral, negative);
                _shopRepository.Update(shop);
            }

            // 8. Save all in one transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new FeedbackDto
            {
                Id = feedback.Id,
                OrderId = feedback.OrderId,
                BuyerId = feedback.BuyerId,
                ShopId = feedback.ShopId,
                Rating = feedback.Rating,
                Comment = feedback.Comment,
                BuyerName = "Buyer", // Mock buyer — sẽ được mask khi seller xem list
                OrderNumber = order.OrderNumber,
                OrderAmount = order.TotalAmount,
                CreatedAt = feedback.CreatedAt
            };
        }
    }
}
