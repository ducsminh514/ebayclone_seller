using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using EbayClone.Shared.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly EbayDbContext _context;

        public OrderRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(idempotencyKey)) return null;
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            // [Performance] Safety cap: tránh load vô hạn records vào RAM
            // Ưu tiên dùng GetPagedOrdersByShopIdAsync thay thế
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.ShopId == shopId)
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(500)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedOrdersByShopIdAsync(
            Guid shopId, 
            int pageNumber, 
            int pageSize, 
            string? status = null, 
            string? searchQuery = null, 
            CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Where(o => o.ShopId == shopId);

            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(o => o.OrderNumber.Contains(searchQuery) || o.Id.ToString().Contains(searchQuery));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            
            var items = await query
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public void Update(Order order)
        {
            _context.Orders.Update(order);
        }

        public async Task<int> CountByStatusAsync(Guid shopId, string status, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Where(o => o.ShopId == shopId && o.Status == status)
                .CountAsync(cancellationToken);
        }

        public async Task<int> CountTotalOrdersAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Where(o => o.ShopId == shopId && o.Status != "CANCELLED")
                .CountAsync(cancellationToken);
        }

        public async Task<decimal> SumSalesAsync(Guid shopId, int days, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await _context.Orders
                .Where(o => o.ShopId == shopId 
                    && (o.Status == "PAID" || o.Status == "SHIPPED" || o.Status == "DELIVERED" || o.Status == "COMPLETED") 
                    && o.CreatedAt >= cutoffDate)
                .SumAsync(o => o.TotalAmount, cancellationToken);
        }

        /// <summary>
        /// [Performance] Single GROUP BY aggregate query cho sales chart.
        /// SQL tương đương: SELECT CAST(PaidAt AS DATE), SUM(TotalAmount), COUNT(*) 
        ///   FROM Orders WHERE ShopId = @id AND PaidAt >= @cutoff GROUP BY CAST(PaidAt AS DATE)
        /// </summary>
        public async Task<List<DailySalesPoint>> GetSalesChartDataAsync(Guid shopId, int days, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-days);
            
            var data = await _context.Orders
                .AsNoTracking()
                .Where(o => o.ShopId == shopId 
                    && o.PaidAt != null
                    && o.PaidAt >= cutoffDate
                    && o.Status != "CANCELLED"
                    && o.Status != "REFUNDED")  // [FIX] Loại cả đơn refund
                .GroupBy(o => o.PaidAt!.Value.Date)
                .Select(g => new DailySalesPoint
                {
                    Date = g.Key,
                    // [FIX-W3] Dùng ItemSubtotal (TotalAmount - ShippingFee) cho doanh số
                    Revenue = g.Sum(o => o.TotalAmount - o.ShippingFee),
                    OrderCount = g.Count()
                })
                .OrderBy(d => d.Date)
                .ToListAsync(cancellationToken);

            return data;
        }

        /// <summary>
        /// [Performance Phase 1] Lấy các đơn DELIVERED đủ điều kiện giải ngân — filter trực tiếp trong SQL.
        /// Hold period: TOP_RATED=0, ABOVE_STANDARD=3, BELOW_STANDARD=14, NEW=21 ngày.
        /// 
        /// Trước đây: LoadAll → filter in-memory → OOM risk khi orders nhiều.
        /// Bây giờ: SQL-side filter bằng CASE WHEN + DATEADD → chỉ load records đủ điều kiện.
        /// </summary>
        public async Task<IEnumerable<Order>> GetOrdersEligibleForFundReleaseAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            // [Performance] Filter trực tiếp trong SQL — không load tất cả vào RAM
            // EF Core sẽ dịch DeliveredAt.Value.AddDays(...) thành DATEADD(day, ...) trong SQL Server
            return await _context.Orders
                .Include(o => o.Shop)
                .Where(o => o.Status == "DELIVERED" 
                    && !o.IsEscrowReleased 
                    && o.DeliveredAt.HasValue
                    && o.Shop != null
                    && (
                        // TOP_RATED: hold 0 ngày → release ngay
                        (o.Shop.SellerLevel == "TOP_RATED" && o.DeliveredAt!.Value <= now) ||
                        // ABOVE_STANDARD: hold 3 ngày
                        (o.Shop.SellerLevel == "ABOVE_STANDARD" && o.DeliveredAt!.Value.AddDays(3) <= now) ||
                        // BELOW_STANDARD: hold 14 ngày
                        (o.Shop.SellerLevel == "BELOW_STANDARD" && o.DeliveredAt!.Value.AddDays(14) <= now) ||
                        // NEW (mặc định): hold 21 ngày
                        (o.Shop.SellerLevel == "NEW" && o.DeliveredAt!.Value.AddDays(21) <= now) ||
                        // Fallback cho SellerLevel không xác định: hold 7 ngày (an toàn)
                        (!new[] { "TOP_RATED", "ABOVE_STANDARD", "BELOW_STANDARD", "NEW" }.Contains(o.Shop.SellerLevel) 
                            && o.DeliveredAt!.Value.AddDays(7) <= now)
                    ))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountCompletedInPeriodAsync(Guid shopId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Where(o => o.ShopId == shopId 
                    && o.PaidAt != null 
                    && o.PaidAt >= from 
                    && o.PaidAt <= to
                    && o.Status != "CANCELLED")
                .CountAsync(cancellationToken);
        }
    }
}
