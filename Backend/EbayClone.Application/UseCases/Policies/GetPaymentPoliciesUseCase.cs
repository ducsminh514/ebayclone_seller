using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface IGetPaymentPoliciesUseCase
    {
        Task<IEnumerable<PaymentPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetPaymentPoliciesUseCase : IGetPaymentPoliciesUseCase
    {
        private readonly IPolicyRepository _policyRepository;

        public GetPaymentPoliciesUseCase(IPolicyRepository policyRepository)
        {
            _policyRepository = policyRepository;
        }

        public async Task<IEnumerable<PaymentPolicyDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var policies = await _policyRepository.GetPaymentPoliciesByShopIdAsync(shopId, cancellationToken);
            return policies.Select(p => new PaymentPolicyDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                ImmediatePaymentRequired = p.ImmediatePaymentRequired,
                DaysToPayment = p.DaysToPayment,
                PaymentMethod = p.PaymentMethod,
                PaymentInstructions = p.PaymentInstructions,
                IsDefault = p.IsDefault,
                RowVersion = p.RowVersion
            }).OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name).ToList();
        }
    }
}
