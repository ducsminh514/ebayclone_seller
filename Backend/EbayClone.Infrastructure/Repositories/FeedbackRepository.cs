using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly EbayDbContext _context;

        public FeedbackRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Feedbacks
                .Include(f => f.Buyer)
                .Include(f => f.Order)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<Feedback?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await _context.Feedbacks
                .Include(f => f.Buyer)
                .Include(f => f.Order)
                .FirstOrDefaultAsync(f => f.OrderId == orderId, cancellationToken);
        }

        public async Task<(IEnumerable<Feedback> Items, int TotalCount)> GetByShopIdPagedAsync(
            Guid shopId, int page, int pageSize, string? ratingFilter = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Feedbacks
                .Where(f => f.ShopId == shopId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(ratingFilter) && ratingFilter != "ALL")
            {
                query = query.Where(f => f.Rating == ratingFilter);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(f => f.Buyer)
                .Include(f => f.Order)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(int Positive, int Neutral, int Negative)> GetShopFeedbackCountsAsync(
            Guid shopId, CancellationToken cancellationToken = default)
        {
            var counts = await _context.Feedbacks
                .Where(f => f.ShopId == shopId)
                .GroupBy(f => f.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return (
                counts.FirstOrDefault(c => c.Rating == FeedbackRatings.POSITIVE)?.Count ?? 0,
                counts.FirstOrDefault(c => c.Rating == FeedbackRatings.NEUTRAL)?.Count ?? 0,
                counts.FirstOrDefault(c => c.Rating == FeedbackRatings.NEGATIVE)?.Count ?? 0
            );
        }

        public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default)
        {
            await _context.Feedbacks.AddAsync(feedback, cancellationToken);
        }

        public void Update(Feedback feedback)
        {
            _context.Feedbacks.Update(feedback);
        }
    }
}
