using EbayClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IFeedbackRepository
    {
        Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Feedback?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);

        Task<(IEnumerable<Feedback> Items, int TotalCount)> GetByShopIdPagedAsync(
            Guid shopId, int page, int pageSize, string? ratingFilter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tính tổng từng loại feedback cho shop (để update denormalized stats).
        /// </summary>
        Task<(int Positive, int Neutral, int Negative)> GetShopFeedbackCountsAsync(
            Guid shopId, CancellationToken cancellationToken = default);

        Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default);
        void Update(Feedback feedback);
    }
}
