using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
using EbayClone.Application.Interfaces.Repositories;
using System.Text.Json;

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
                Description = p.Description ?? string.Empty,
                HandlingTimeDays = p.HandlingTimeDays,
                ShippingMethod = p.ShippingMethod,
                OfferLocalPickup = p.OfferLocalPickup,
                OfferFreeShipping = p.OfferFreeShipping,
                DomesticCostType = p.DomesticCostType,
                DomesticServices = JsonSerializer.Deserialize<List<ShippingServiceDto>>(p.DomesticServicesJson) ?? new(),
                IsInternationalShippingAllowed = p.IsInternationalShippingAllowed,
                InternationalCostType = p.InternationalCostType,
                InternationalServices = JsonSerializer.Deserialize<List<InternationalShippingServiceDto>>(p.InternationalServicesJson) ?? new(),
                OfferCombinedShippingDiscount = p.OfferCombinedShippingDiscount,
                PackageType = p.PackageType,
                PackageWeightOz = p.PackageWeightOz,
                PackageDimensionsJson = p.PackageDimensionsJson,
                HandlingTimeCutoff = p.HandlingTimeCutoff,
                ExcludedLocations = JsonSerializer.Deserialize<List<string>>(p.ExcludedLocationsJson) ?? new(),
                IsDefault = p.IsDefault,
                RowVersion = p.RowVersion
            }).OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name).ToList();
        }
    }
}
