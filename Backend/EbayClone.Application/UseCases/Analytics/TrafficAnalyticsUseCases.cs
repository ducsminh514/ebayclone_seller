using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Analytics
{
    // ─── Track Product View ───

    public interface ITrackProductViewUseCase
    {
        Task<bool> ExecuteAsync(Guid productId, string? viewerIp, CancellationToken ct = default);
    }

    public class TrackProductViewUseCase : ITrackProductViewUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductViewLogRepository _viewLogRepository;

        public TrackProductViewUseCase(
            IProductRepository productRepository,
            IProductViewLogRepository viewLogRepository)
        {
            _productRepository = productRepository;
            _viewLogRepository = viewLogRepository;
        }

        /// <summary>
        /// Track product view với rate limit: 1 view/IP/product/hour.
        /// Returns true nếu view được ghi nhận, false nếu bị rate limit.
        /// </summary>
        public async Task<bool> ExecuteAsync(Guid productId, string? viewerIp, CancellationToken ct = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, ct);
            if (product == null || product.IsDeleted || product.Status != "ACTIVE")
                return false;

            // Rate limit: 1 view per IP per product per hour
            var ip = viewerIp ?? "anonymous";
            if (await _viewLogRepository.HasRecentViewAsync(productId, ip, TimeSpan.FromHours(1), ct))
                return false;

            await _viewLogRepository.AddAsync(new ProductViewLog
            {
                ShopId = product.ShopId,
                ProductId = productId,
                ViewerIP = ip
            }, ct);

            return true;
        }
    }

    // ─── Get Traffic Stats ───

    public interface IGetTrafficStatsUseCase
    {
        Task<TrafficStatsDto> ExecuteAsync(Guid shopId, int days, CancellationToken ct = default);
    }

    public class TrafficStatsDto
    {
        public int TotalViews { get; set; }
        public int TotalUniqueViewers { get; set; }
        public List<DailyViewPoint> DailyViews { get; set; } = new();
        public List<TopViewedProduct> TopProducts { get; set; } = new();
    }

    public class GetTrafficStatsUseCase : IGetTrafficStatsUseCase
    {
        private readonly IProductViewLogRepository _viewLogRepository;

        public GetTrafficStatsUseCase(IProductViewLogRepository viewLogRepository)
        {
            _viewLogRepository = viewLogRepository;
        }

        public async Task<TrafficStatsDto> ExecuteAsync(Guid shopId, int days, CancellationToken ct = default)
        {
            // Sequential: DbContext KHÔNG thread-safe
            var dailyViews = await _viewLogRepository.GetDailyViewsAsync(shopId, days, ct);
            var topProducts = await _viewLogRepository.GetTopViewedProductsAsync(shopId, days, 5, ct);

            return new TrafficStatsDto
            {
                TotalViews = dailyViews.Sum(d => d.Views),
                TotalUniqueViewers = dailyViews.Sum(d => d.UniqueViewers),
                DailyViews = dailyViews,
                TopProducts = topProducts
            };
        }
    }
}
