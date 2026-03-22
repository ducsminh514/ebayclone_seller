using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Infrastructure.Data;
using EbayClone.Shared.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class ShopAnalyticsDailyRepository : IShopAnalyticsDailyRepository
    {
        private readonly EbayDbContext _context;

        public ShopAnalyticsDailyRepository(EbayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy daily stats từ ShopAnalyticsDaily table.
        /// Fallback: nếu chưa có data (job chưa chạy) → return empty list.
        /// Dashboard sẽ fallback sang query Orders trực tiếp.
        /// </summary>
        public async Task<List<DailySalesPoint>> GetDailyStatsAsync(Guid shopId, int days, CancellationToken ct = default)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            return await _context.Set<Domain.Entities.ShopAnalyticsDaily>()
                .Where(x => x.ShopId == shopId && x.ReportDate >= startDate)
                .OrderBy(x => x.ReportDate)
                .Select(x => new DailySalesPoint
                {
                    Date = x.ReportDate,
                    Revenue = x.TotalRevenue,
                    OrderCount = x.TotalOrders
                })
                .ToListAsync(ct);
        }
    }
}
