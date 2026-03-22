using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IProductViewLogRepository
    {
        Task AddAsync(ProductViewLog viewLog, CancellationToken ct = default);

        /// <summary>
        /// Rate limit check: đã có view từ IP này trong 1h chưa?
        /// </summary>
        Task<bool> HasRecentViewAsync(Guid productId, string ip, TimeSpan window, CancellationToken ct = default);

        /// <summary>
        /// Traffic stats: views by day cho dashboard.
        /// </summary>
        Task<List<DailyViewPoint>> GetDailyViewsAsync(Guid shopId, int days, CancellationToken ct = default);

        /// <summary>
        /// Top products by views for a shop.
        /// </summary>
        Task<List<TopViewedProduct>> GetTopViewedProductsAsync(Guid shopId, int days, int top, CancellationToken ct = default);
    }

    // DTOs cho traffic
    public class DailyViewPoint
    {
        public DateTime Date { get; set; }
        public int Views { get; set; }
        public int UniqueViewers { get; set; }
    }

    public class TopViewedProduct
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Views { get; set; }
    }
}
