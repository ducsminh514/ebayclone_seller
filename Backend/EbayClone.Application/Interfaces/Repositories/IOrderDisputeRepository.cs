using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IOrderDisputeRepository
    {
        Task<OrderDispute?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OrderDispute?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDispute>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task AddAsync(OrderDispute dispute, CancellationToken cancellationToken = default);
        void Update(OrderDispute dispute);
    }
}
