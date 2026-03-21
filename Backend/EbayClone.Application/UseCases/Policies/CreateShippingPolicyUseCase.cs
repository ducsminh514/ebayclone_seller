using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using System.Text.Json;

namespace EbayClone.Application.UseCases.Policies
{
    public interface ICreateShippingPolicyUseCase
    {
        Task<Guid> ExecuteAsync(Guid shopId, CreateShippingPolicyRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateShippingPolicyUseCase : ICreateShippingPolicyUseCase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateShippingPolicyUseCase(
            IPolicyRepository policyRepository, 
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreateShippingPolicyRequest request, CancellationToken cancellationToken = default)
        {
            // Bảo vệ Database tuyệt đối chống Race Condition TOCTOU:
            // Khóa phạm vi bằng mức IsolationLevel.Serializable để các request query đếm số lượng sẽ phải xếp hàng
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            try
            {
                var currentPolicyCount = await _policyRepository.CountShippingPoliciesAsync(shopId, cancellationToken);
                if (currentPolicyCount >= 100)
                {
                    throw new InvalidOperationException("You have reached the maximum limit of 100 shipping policies.");
                }

                // ===== Business Validation theo ShippingMethod =====
                var validMethods = new[] { "Standard", "Freight", "NoShipping" };
                if (!validMethods.Contains(request.ShippingMethod))
                {
                    throw new ArgumentException($"Invalid shipping method '{request.ShippingMethod}'. Must be: Standard, Freight, or NoShipping.");
                }

                // Khi Standard → validate domestic + (optional) international
                if (request.ShippingMethod == "Standard")
                {
                    // DomesticCostType whitelist 
                    if (request.DomesticCostType != "Flat" && request.DomesticCostType != "Calculated")
                    {
                        throw new ArgumentException("Cost type must be Flat or Calculated for standard shipping.");
                    }

                    // Phải có ít nhất 1 domestic service
                    if (request.DomesticServices == null || request.DomesticServices.Count == 0)
                    {
                        throw new ArgumentException("At least one domestic shipping service must be provided.");
                    }

                    // [FIX-M5] Validate shipping costs không được âm
                    foreach (var svc in request.DomesticServices)
                    {
                        if (svc.Cost < 0)
                            throw new ArgumentException($"Shipping cost for '{svc.ServiceName}' cannot be negative.");
                        if (svc.AdditionalItemCost < 0)
                            throw new ArgumentException($"Additional item cost for '{svc.ServiceName}' cannot be negative.");
                    }

                    // Nếu bật international → phải có ít nhất 1 international service
                    if (request.IsInternationalShippingAllowed 
                        && (request.InternationalServices == null || request.InternationalServices.Count == 0))
                    {
                        throw new ArgumentException("At least one international shipping service must be provided when international shipping is enabled.");
                    }

                    // [FIX-M5] Validate international service costs
                    if (request.IsInternationalShippingAllowed && request.InternationalServices != null)
                    {
                        foreach (var svc in request.InternationalServices)
                        {
                            if (svc.Cost < 0)
                                throw new ArgumentException($"International shipping cost for '{svc.ServiceName}' cannot be negative.");
                            if (svc.AdditionalItemCost < 0)
                                throw new ArgumentException($"International additional item cost for '{svc.ServiceName}' cannot be negative.");
                        }
                    }
                }
                // Khi Freight/NoShipping → domestic/international không cần, clear data
                // (giữ clean, không lưu services rác vào DB khi method không cần)

                // Dập cờ IsDefault cũ để bảo vệ Data Integrity
                if (request.IsDefault)
                {
                    await _policyRepository.ClearDefaultShippingPolicyAsync(shopId, cancellationToken);
                }

                var policy = new ShippingPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    HandlingTimeDays = request.ShippingMethod == "Standard" ? request.HandlingTimeDays : 0,
                    IsDefault = request.IsDefault,

                    ShippingMethod = request.ShippingMethod,
                    // NoShipping = "Local pickup only" → force OfferLocalPickup = true
                    OfferLocalPickup = request.ShippingMethod == "NoShipping" ? true : request.OfferLocalPickup,
                    OfferFreeShipping = request.ShippingMethod == "Standard" ? request.OfferFreeShipping : false,

                    DomesticCostType = request.ShippingMethod == "Standard" ? request.DomesticCostType : "Flat",
                    DomesticServicesJson = request.ShippingMethod == "Standard" 
                        ? JsonSerializer.Serialize(request.DomesticServices) : "[]",

                    IsInternationalShippingAllowed = request.ShippingMethod == "Standard" 
                        ? request.IsInternationalShippingAllowed : false,
                    InternationalCostType = request.InternationalCostType,
                    InternationalServicesJson = request.ShippingMethod == "Standard" && request.IsInternationalShippingAllowed
                        ? JsonSerializer.Serialize(request.InternationalServices) : "[]",

                    OfferCombinedShippingDiscount = request.OfferCombinedShippingDiscount,

                    PackageType = request.PackageType,
                    PackageWeightOz = request.PackageWeightOz,
                    PackageDimensionsJson = request.PackageDimensionsJson,

                    HandlingTimeCutoff = request.HandlingTimeCutoff,

                    ExcludedLocationsJson = JsonSerializer.Serialize(request.ExcludedLocations),

                    IsArchived = false
                };

                await _policyRepository.AddShippingPolicyAsync(policy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Cần lưu trước để Policy sinh ra ID
                
                // Tăng bộ đếm bằng lệnh T-SQL Atomic chống Race Condition thay vì (shop.Total += 1)
                await _shopRepository.IncrementTotalShippingPoliciesAsync(shopId, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return policy.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to create Shipping Policy: {ex.Message}", ex);
            }
        }
    }
}
