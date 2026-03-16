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
    public class OrderReturnRepository : IOrderReturnRepository
    {
        private readonly EbayDbContext _context;

        public OrderReturnRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<OrderReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.OrderReturns
                .Include(r => r.Order)
                    .ThenInclude(o => o!.Items)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        /// <summary>
        /// Lấy return request CHƯA KẾT THÚC (REQUESTED/ACCEPTED/IN_PROGRESS) cho 1 order.
        /// </summary>
        public async Task<OrderReturn?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderReturns
                .Include(r => r.Order)
                .Where(r => r.OrderId == orderId 
                    && r.Status != "REFUNDED" 
                    && r.Status != "PARTIALLY_REFUNDED" 
                    && r.Status != "CLOSED")
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<OrderReturn>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.OrderReturns
                .Include(r => r.Order)
                .Where(r => r.Order!.ShopId == shopId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(OrderReturn orderReturn, CancellationToken cancellationToken = default)
        {
            await _context.OrderReturns.AddAsync(orderReturn, cancellationToken);
        }

        public void Update(OrderReturn orderReturn)
        {
            _context.OrderReturns.Update(orderReturn);
        }
    }
}
