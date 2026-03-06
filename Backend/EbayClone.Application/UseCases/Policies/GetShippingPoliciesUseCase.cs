using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Policies;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface IGetShippingPoliciesUseCase
    {
        Task<IEnumerable<ShippingPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetShippingPoliciesUseCase : IGetShippingPoliciesUseCase
    {
        private readonly IPolicyRepository _policyRepository;

        public GetShippingPoliciesUseCase(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task<IEnumerable<ShippingPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetShippingPoliciesByShopIdAsync(shopId, cancellationToken);
            return policies.Select(p => new ShippingPolicyDto
            {
                Id = p.Id,
                Name = p.Name,
                HandlingTimeDays = p.HandlingTimeDays,
                Cost = p.Cost,
                IsDefault = p.IsDefault
            }).OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name).ToList();
        }
    }
}
