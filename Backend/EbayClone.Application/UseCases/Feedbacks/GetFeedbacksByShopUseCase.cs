using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Common;
using EbayClone.Shared.DTOs.Feedbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Feedbacks
{
    public interface IGetFeedbacksByShopUseCase
    {
        Task<PagedResult<FeedbackDto>> ExecuteAsync(
            Guid shopId, int page, int pageSize, string? ratingFilter = null,
            CancellationToken cancellationToken = default);
    }

    public class GetFeedbacksByShopUseCase : IGetFeedbacksByShopUseCase
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public GetFeedbacksByShopUseCase(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        public async Task<PagedResult<FeedbackDto>> ExecuteAsync(
            Guid shopId, int page, int pageSize, string? ratingFilter = null,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _feedbackRepository
                .GetByShopIdPagedAsync(shopId, page, pageSize, ratingFilter, cancellationToken);

            return new PagedResult<FeedbackDto>
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        private FeedbackDto MapToDto(Feedback f)
        {
            return new FeedbackDto
            {
                Id = f.Id,
                OrderId = f.OrderId,
                BuyerId = f.BuyerId,
                ShopId = f.ShopId,
                Rating = f.Rating,
                Comment = f.Comment,
                BuyerName = MaskBuyerName(f.Buyer?.Username ?? f.Buyer?.FullName ?? "Buyer"),
                OrderNumber = f.Order?.OrderNumber,
                OrderAmount = f.Order?.TotalAmount ?? 0,
                SellerReply = f.SellerReply,
                SellerRepliedAt = f.SellerRepliedAt,
                CreatedAt = f.CreatedAt
            };
        }

        /// <summary>
        /// Ẩn tên buyer: "ducminh" → "d***h"
        /// </summary>
        private string MaskBuyerName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length <= 2)
                return "***";

            return $"{name[0]}***{name[^1]}";
        }
    }
}
