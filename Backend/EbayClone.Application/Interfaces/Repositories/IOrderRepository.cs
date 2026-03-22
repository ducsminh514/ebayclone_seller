using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Dashboard;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedOrdersByShopIdAsync(
            Guid shopId, 
            int pageNumber, 
            int pageSize, 
            string? status = null, 
            string? searchQuery = null, 
            CancellationToken cancellationToken = default);
        Task AddAsync(Order order, CancellationToken cancellationToken = default);
        void Update(Order order);

        // Dashboard Stats
        Task<int> CountByStatusAsync(Guid shopId, string status, CancellationToken cancellationToken = default);
        Task<int> CountTotalOrdersAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<decimal> SumSalesAsync(Guid shopId, int days, CancellationToken cancellationToken = default);

        /// <summary>
        /// Single aggregate query: GROUP BY date → sales chart data.
        /// Performance: 1 query thay vì N queries cho mỗi ngày.
        /// </summary>
        Task<List<DailySalesPoint>> GetSalesChartDataAsync(Guid shopId, int days, CancellationToken cancellationToken = default);

        // Financials
        Task<IEnumerable<Order>> GetOrdersEligibleForFundReleaseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm transactions (non-cancelled) trong evaluation period cho Seller Level evaluation.
        /// </summary>
        Task<int> CountCompletedInPeriodAsync(Guid shopId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    }
}
