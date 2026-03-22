using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class ProductViewLogRepository : IProductViewLogRepository
    {
        private readonly EbayDbContext _context;

        public ProductViewLogRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ProductViewLog viewLog, CancellationToken ct = default)
        {
            await _context.Set<ProductViewLog>().AddAsync(viewLog, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> HasRecentViewAsync(Guid productId, string ip, TimeSpan window, CancellationToken ct = default)
        {
            var cutoff = DateTimeOffset.UtcNow - window;
            return await _context.Set<ProductViewLog>()
                .AnyAsync(v => v.ProductId == productId && v.ViewerIP == ip && v.ViewedAt >= cutoff, ct);
        }

        public async Task<List<DailyViewPoint>> GetDailyViewsAsync(Guid shopId, int days, CancellationToken ct = default)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);

            return await _context.Set<ProductViewLog>()
                .Where(v => v.ShopId == shopId && v.ViewedAt >= startDate)
                .GroupBy(v => v.ViewedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyViewPoint
                {
                    Date = g.Key,
                    Views = g.Count(),
                    UniqueViewers = g.Select(v => v.ViewerIP).Distinct().Count()
                })
                .ToListAsync(ct);
        }

        public async Task<List<TopViewedProduct>> GetTopViewedProductsAsync(Guid shopId, int days, int top, CancellationToken ct = default)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);

            return await _context.Set<ProductViewLog>()
                .Where(v => v.ShopId == shopId && v.ViewedAt >= startDate)
                .GroupBy(v => new { v.ProductId })
                .OrderByDescending(g => g.Count())
                .Take(top)
                .Select(g => new TopViewedProduct
                {
                    ProductId = g.Key.ProductId,
                    Views = g.Count()
                })
                .ToListAsync(ct);
        }
    }
}
