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
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.ShopId == shopId)
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
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
        // eBay Policy: Release after 3-7 days post-delivery.
        // TODO: Dùng config/appsettings cho production. Demo = 1 phút.
        private const int FUND_RELEASE_DELAY_MINUTES = 1;
        
        public async Task<IEnumerable<Order>> GetOrdersEligibleForFundReleaseAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-FUND_RELEASE_DELAY_MINUTES); 
            
            return await _context.Orders
                .Where(o => o.Status == "DELIVERED" && o.DeliveredAt <= cutoff && !o.IsEscrowReleased) 
                .ToListAsync(cancellationToken);
        }
    }
}
