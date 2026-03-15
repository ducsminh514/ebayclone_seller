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
        Task ClearDefaultReturnPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);

        Task AddShippingPolicyAsync(ShippingPolicy policy, CancellationToken cancellationToken = default);
        Task AddReturnPolicyAsync(ReturnPolicy policy, CancellationToken cancellationToken = default);

        Task<IEnumerable<ShippingPolicy>> GetShippingPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<IEnumerable<ReturnPolicy>> GetReturnPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);

        // Payment Policy
        Task AddPaymentPolicyAsync(PaymentPolicy policy, CancellationToken cancellationToken = default);
        Task<IEnumerable<PaymentPolicy>> GetPaymentPoliciesByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<int> CountPaymentPoliciesAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task ClearDefaultPaymentPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);
        // Default Recovery (Fallback Logic)
        Task<ShippingPolicy?> GetDefaultShippingPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<ReturnPolicy?> GetDefaultReturnPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task<PaymentPolicy?> GetDefaultPaymentPolicyAsync(Guid shopId, CancellationToken cancellationToken = default);

        // GetById
        Task<ShippingPolicy?> GetShippingPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
        Task<ReturnPolicy?> GetReturnPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);
        Task<PaymentPolicy?> GetPaymentPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default);

        // Delete
        Task DeleteShippingPolicyAsync(ShippingPolicy policy, CancellationToken cancellationToken = default);
        Task DeleteReturnPolicyAsync(ReturnPolicy policy, CancellationToken cancellationToken = default);
        Task DeletePaymentPolicyAsync(PaymentPolicy policy, CancellationToken cancellationToken = default);

        // Update
        void UpdateShippingPolicy(ShippingPolicy policy);
        void UpdateReturnPolicy(ReturnPolicy policy);
        void UpdatePaymentPolicy(PaymentPolicy policy);
    }
}
