using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IShopRepository
    {
        Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Shop?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task AddAsync(Shop shop, CancellationToken cancellationToken = default);
        void Update(Shop shop);
        Task IncrementTotalShippingPoliciesAsync(Guid id, CancellationToken cancellationToken = default);
        Task IncrementTotalReturnPoliciesAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
