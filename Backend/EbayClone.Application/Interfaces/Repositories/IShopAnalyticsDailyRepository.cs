using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Dashboard;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IShopAnalyticsDailyRepository
    {
        /// <summary>
        /// Lấy analytics daily data cho dashboard sales chart.
        /// Performance: query 31 rows fixed thay vì scan thousands Orders.
        /// </summary>
        Task<List<DailySalesPoint>> GetDailyStatsAsync(Guid shopId, int days, CancellationToken ct = default);
    }
}
