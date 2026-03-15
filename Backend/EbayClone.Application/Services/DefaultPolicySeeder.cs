using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Services
{
    public interface IDefaultPolicySeeder
    {
        Task SeedDefaultPoliciesAsync(Guid? shopId, CancellationToken cancellationToken = default);
    }

    public class DefaultPolicySeeder : IDefaultPolicySeeder
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;

        public DefaultPolicySeeder(IPolicyRepository policyRepository, IShopRepository shopRepository)
        {
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
        }

        public async Task SeedDefaultPoliciesAsync(Guid? shopId, CancellationToken cancellationToken = default)
        {
            // 1. Default Shipping Policy
            var defaultShipping = new ShippingPolicy
            {
                ShopId = shopId,
                Name = "Standard Shipping",
                Description = "Default shipping policy (Free 2-5 business days)",
                HandlingTimeDays = 2,
                IsDefault = true,
                DomesticCostType = "Flat",
                DomesticServicesJson = "[{\"ServiceId\":\"usps_ground_adv\",\"ServiceName\":\"USPS Ground Advantage (2-5 business days)\",\"Cost\":0,\"AdditionalItemCost\":0,\"IsFreeShipping\":true}]",
                InternationalServicesJson = "[]",
                ExcludedLocationsJson = "[]"
            };
            await _policyRepository.AddShippingPolicyAsync(defaultShipping, cancellationToken);

            // 2. Default Return Policy
            var defaultReturn = new ReturnPolicy
            {
                ShopId = shopId,
                Name = "30-day Returns",
                Description = "Default return policy",
                IsDefault = true,
                IsDomesticAccepted = true,
                DomesticReturnDays = 30,
                DomesticShippingPaidBy = "BUYER",
                IsInternationalAccepted = false
            };
            await _policyRepository.AddReturnPolicyAsync(defaultReturn, cancellationToken);

            // 3. Default Payment Policy (Managed Payments)
            var defaultPayment = new PaymentPolicy
            {
                ShopId = shopId,
                Name = "eBay Managed Payments",
                Description = "Default payment policy using eBay Managed Payments",
                IsDefault = true,
                PaymentMethod = "eBay Managed Payments",
                ImmediatePaymentRequired = true
            };
            await _policyRepository.AddPaymentPolicyAsync(defaultPayment, cancellationToken);

            // Cập nhật counters cho Shop nếu có shopId
            if (shopId.HasValue)
            {
                await _shopRepository.IncrementTotalShippingPoliciesAsync(shopId.Value, cancellationToken);
                await _shopRepository.IncrementTotalReturnPoliciesAsync(shopId.Value, cancellationToken);
                await _shopRepository.IncrementTotalPaymentPoliciesAsync(shopId.Value, cancellationToken);
            }
        }
    }
}
