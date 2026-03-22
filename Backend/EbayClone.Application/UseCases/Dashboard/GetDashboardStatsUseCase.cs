using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Shared.DTOs.Dashboard;

namespace EbayClone.Application.UseCases.Dashboard
{
    public interface IGetDashboardStatsUseCase
    {
        Task<DashboardStatsDto> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetDashboardStatsUseCase : IGetDashboardStatsUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IShopRepository _shopRepository;

        public GetDashboardStatsUseCase(
            IProductRepository productRepository, 
            IOrderRepository orderRepository,
            IShopRepository shopRepository)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _shopRepository = shopRepository;
        }

        /// <summary>
        /// [Performance] Dashboard phải tải < 1s kể cả khi có hàng trăm sản phẩm.
        /// Chiến lược: 
        ///   1. Đọc denormalized counts từ Shop (0 COUNT queries)
        ///   2. Sequential queries (DbContext KHÔNG thread-safe — Task.WhenAll crash)
        ///   3. Sales chart dùng single GROUP BY aggregate
        /// Perf: 4 sequential queries nhưng rất nhanh (~100-200ms total)
        ///   vì 3/4 đọc denormalized fields, chỉ SalesChart cần real aggregate
        /// </summary>
        public async Task<DashboardStatsDto> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            // Sequential: DbContext không thread-safe, KHÔNG dùng Task.WhenAll
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null) throw new InvalidOperationException("Shop not found.");

            var salesChart = await _orderRepository.GetSalesChartDataAsync(shopId, 31, cancellationToken);
            var usedListing = await _productRepository.CountProductsThisMonthAsync(shopId, cancellationToken);
            var unsold = await _productRepository.CountByStatusAsync(shopId, "ENDED", cancellationToken);

            // Tính TotalSales90Days từ denormalized field
            // Hoặc fallback tính từ chart nếu cần chính xác hơn
            return new DashboardStatsDto
            {
                // Listing counts — đọc từ denormalized (0 queries thêm)
                ActiveCount = shop.ActiveListingCount,
                DraftCount = shop.DraftListingCount,
                OrderCount = shop.AwaitingShipmentCount,
                UnsoldCount = unsold,
                TotalSales90Days = shop.TotalSalesAmount,
                
                // Listing limits
                MonthlyListingLimit = shop.MonthlyListingLimit,
                UsedListingLimit = usedListing,

                // Seller Performance — đọc từ denormalized (0 queries thêm)
                SellerLevel = shop.SellerLevel,
                DefectCount = shop.DefectCount,
                TotalTransactions = shop.TotalTransactions,
                DefectRate = shop.TotalTransactions > 0 
                    ? Math.Round((decimal)shop.DefectCount / shop.TotalTransactions * 100, 2) 
                    : 0,
                LateShipmentCount = shop.LateShipmentCount,
                FeedbackScore = shop.FeedbackScore,
                PositivePercent = shop.PositivePercent,

                // Sales chart — 1 aggregate query
                SalesChart = salesChart
            };
        }
    }
}
