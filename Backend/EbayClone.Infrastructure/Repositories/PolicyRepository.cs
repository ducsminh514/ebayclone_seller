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

        public async Task ClearDefaultShippingPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            await _context.ShippingPolicies
                .Where(p => p.ShopId == shopId && p.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), cancellationToken);
        }

        public async Task ClearDefaultReturnPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            await _context.ReturnPolicies
                .Where(p => p.ShopId == shopId && p.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), cancellationToken);
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
                .AsNoTracking()
                .Where(p => p.ShopId == shopId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<ReturnPolicy>> GetReturnPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.ReturnPolicies
                .AsNoTracking()
                .Where(p => p.ShopId == shopId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddPaymentPolicyAsync(PaymentPolicy policy, CancellationToken cancellationToken = default)
        {
            await _context.PaymentPolicies.AddAsync(policy, cancellationToken);
        }

        public async Task<IEnumerable<PaymentPolicy>> GetPaymentPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.PaymentPolicies
                .AsNoTracking()
                .Where(p => p.ShopId == shopId)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountPaymentPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.PaymentPolicies.CountAsync(p => p.ShopId == shopId, cancellationToken);
        }

        public async Task ClearDefaultPaymentPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            await _context.PaymentPolicies
                .Where(p => p.ShopId == shopId && p.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDefault, false), cancellationToken);
        }

        public async Task<ShippingPolicy?> GetDefaultShippingPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var shopDefault = await _context.ShippingPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == shopId && p.IsDefault, cancellationToken);
            if (shopDefault != null) return shopDefault;

            return await _context.ShippingPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == null && p.IsDefault, cancellationToken);
        }

        public async Task<ReturnPolicy?> GetDefaultReturnPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var shopDefault = await _context.ReturnPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == shopId && p.IsDefault, cancellationToken);
            if (shopDefault != null) return shopDefault;

            return await _context.ReturnPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == null && p.IsDefault, cancellationToken);
        }

        public async Task<PaymentPolicy?> GetDefaultPaymentPolicyAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var shopDefault = await _context.PaymentPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == shopId && p.IsDefault, cancellationToken);
            if (shopDefault != null) return shopDefault;

            return await _context.PaymentPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShopId == null && p.IsDefault, cancellationToken);
        }

        // GetById (tracked for update/delete)
        public async Task<ShippingPolicy?> GetShippingPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
            => await _context.ShippingPolicies.FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);

        public async Task<ReturnPolicy?> GetReturnPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
            => await _context.ReturnPolicies.FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);

        public async Task<PaymentPolicy?> GetPaymentPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
            => await _context.PaymentPolicies.FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);

        // Delete
        public Task DeleteShippingPolicyAsync(ShippingPolicy policy, CancellationToken cancellationToken = default)
        {
            _context.ShippingPolicies.Remove(policy);
            return Task.CompletedTask;
        }

        public Task DeleteReturnPolicyAsync(ReturnPolicy policy, CancellationToken cancellationToken = default)
        {
            _context.ReturnPolicies.Remove(policy);
            return Task.CompletedTask;
        }

        public Task DeletePaymentPolicyAsync(PaymentPolicy policy, CancellationToken cancellationToken = default)
        {
            _context.PaymentPolicies.Remove(policy);
            return Task.CompletedTask;
        }

        // Update (EF Core tracks changes automatically)
        public void UpdateShippingPolicy(ShippingPolicy policy) => _context.ShippingPolicies.Update(policy);
        public void UpdateReturnPolicy(ReturnPolicy policy) => _context.ReturnPolicies.Update(policy);
        public void UpdatePaymentPolicy(PaymentPolicy policy) => _context.PaymentPolicies.Update(policy);
    }
}
