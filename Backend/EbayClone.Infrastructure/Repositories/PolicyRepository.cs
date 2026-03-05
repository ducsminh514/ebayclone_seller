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
    public class PolicyRepository : IPolicyRepository
    {
        private readonly EbayDbContext _context;

        public PolicyRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<int> CountShippingPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingPolicies.CountAsync(p => p.ShopId == shopId, cancellationToken);
        }

        public async Task<int> CountReturnPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.ReturnPolicies.CountAsync(p => p.ShopId == shopId, cancellationToken);
        }

        public async Task AddShippingPolicyAsync(ShippingPolicy policy, CancellationToken cancellationToken = default)
        {
            await _context.ShippingPolicies.AddAsync(policy, cancellationToken);
        }

        public async Task AddReturnPolicyAsync(ReturnPolicy policy, CancellationToken cancellationToken = default)
        {
            await _context.ReturnPolicies.AddAsync(policy, cancellationToken);
        }

        public async Task<IEnumerable<ShippingPolicy>> GetShippingPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.ShippingPolicies
                .Where(p => p.ShopId == shopId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ReturnPolicy>> GetReturnPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.ReturnPolicies
                .Where(p => p.ShopId == shopId)
                .ToListAsync(cancellationToken);
        }
    }
}
