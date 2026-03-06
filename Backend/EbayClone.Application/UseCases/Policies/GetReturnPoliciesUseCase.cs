using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Policies;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface IGetReturnPoliciesUseCase
    {
        Task<IEnumerable<ReturnPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetReturnPoliciesUseCase : IGetReturnPoliciesUseCase
    {
        private readonly IPolicyRepository _policyRepository;

        public GetReturnPoliciesUseCase(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task<IEnumerable<ReturnPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetReturnPoliciesByShopIdAsync(shopId, cancellationToken);
            return policies.Select(p => new ReturnPolicyDto
            {
                Id = p.Id,
                Name = p.Name,
                ReturnDays = p.ReturnDays,
                ShippingPaidBy = p.ShippingPaidBy
            }).OrderBy(p => p.Name).ToList();
        }
    }
}
