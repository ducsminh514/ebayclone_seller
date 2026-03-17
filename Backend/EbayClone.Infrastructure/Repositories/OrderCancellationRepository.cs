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
    public class OrderCancellationRepository : IOrderCancellationRepository
    {
        private readonly EbayDbContext _context;

        public OrderCancellationRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<OrderCancellation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.OrderCancellations
                .Include(c => c.Order)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<OrderCancellation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderCancellations
                .Include(c => c.Order)
                .Where(c => c.OrderId == orderId && c.Status != "DECLINED")
                .OrderByDescending(c => c.RequestedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrderCancellation>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderCancellations
                .Include(c => c.Order)
                .Where(c => c.Order!.ShopId == shopId)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(OrderCancellation cancellation, CancellationToken cancellationToken = default)
        {
            await _context.OrderCancellations.AddAsync(cancellation, cancellationToken);
        }

        public void Update(OrderCancellation cancellation)
        {
            _context.OrderCancellations.Update(cancellation);
        }
    }
}
