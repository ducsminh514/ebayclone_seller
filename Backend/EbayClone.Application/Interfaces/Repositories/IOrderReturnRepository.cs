using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IOrderReturnRepository
    {
        Task<OrderReturn?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OrderReturn?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderReturn>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task AddAsync(OrderReturn orderReturn, CancellationToken cancellationToken = default);
        void Update(OrderReturn orderReturn);
    }
}
