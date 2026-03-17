using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IOrderCancellationRepository
    {
        Task<OrderCancellation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<OrderCancellation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderCancellation>> GetByShopOrdersAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task AddAsync(OrderCancellation cancellation, CancellationToken cancellationToken = default);
        void Update(OrderCancellation cancellation);
    }
}
