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

        public async Task<DashboardStatsDto> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null) throw new InvalidOperationException("Shop not found.");

            var stats = new DashboardStatsDto
            {
                ActiveCount = await _productRepository.CountByStatusAsync(shopId, "ACTIVE", cancellationToken),
                DraftCount = await _productRepository.CountByStatusAsync(shopId, "DRAFT", cancellationToken),
                OrderCount = await _orderRepository.CountByStatusAsync(shopId, "READY_TO_SHIP", cancellationToken), // Awaiting shipment
                UnsoldCount = await _productRepository.CountByStatusAsync(shopId, "ENDED", cancellationToken),
                TotalSales90Days = await _orderRepository.SumSalesAsync(shopId, 90, cancellationToken),
                MonthlyListingLimit = shop.MonthlyListingLimit,
                UsedListingLimit = await _productRepository.CountProductsThisMonthAsync(shopId, cancellationToken)
            };

            return stats;
        }
    }
}
