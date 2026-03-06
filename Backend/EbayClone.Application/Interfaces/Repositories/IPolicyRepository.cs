using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IPolicyRepository
    {
        Task<int> CountShippingPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<int> CountReturnPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default);
        
        Task ClearDefaultShippingPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);

        Task AddShippingPolicyAsync(ShippingPolicy policy, CancellationToken cancellationToken = default);
        Task AddReturnPolicyAsync(ReturnPolicy policy, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingPolicy>> GetShippingPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReturnPolicy>> GetReturnPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    }
}
