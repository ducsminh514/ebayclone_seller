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
    public class OrderDisputeRepository : IOrderDisputeRepository
    {
        private readonly EbayDbContext _context;

        public OrderDisputeRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.OrderDisputes
                .Include(d => d.Order)
                    .ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        /// <summary>
        /// Lấy dispute CHƯA KẾT THÚC cho 1 order.
        /// </summary>
        public async Task<OrderDispute?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderDisputes
                .Include(d => d.Order)
                .Where(d => d.OrderId == orderId
                    && d.Status != "RESOLVED_BUYER_WIN"
                    && d.Status != "RESOLVED_SELLER_WIN")
                .OrderByDescending(d => d.OpenedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrderDispute>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderDisputes
                .Include(d => d.Order)
                .Where(d => d.Order!.ShopId == shopId)
                .OrderByDescending(d => d.OpenedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(OrderDispute dispute, CancellationToken cancellationToken = default)
        {
            await _context.OrderDisputes.AddAsync(dispute, cancellationToken);
        }

        public void Update(OrderDispute dispute)
        {
            _context.OrderDisputes.Update(dispute);
        }
    }
}
