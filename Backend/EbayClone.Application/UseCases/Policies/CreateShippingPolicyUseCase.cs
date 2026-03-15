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

                // Business Validation: Phải có ít nhất 1 dịch vụ vận chuyển nếu không phải là Local Pickup
                if (request.DomesticCostType != "NoShipping" && (request.DomesticServices == null || request.DomesticServices.Count == 0))
                {
                    throw new ArgumentException("At least one domestic shipping service must be provided.");
                }
                // Dập cờ IsDefault cũ để bảo vệ mảng Data Integrity
                if (request.IsDefault)
                {
                    await _policyRepository.ClearDefaultShippingPolicyAsync(shopId, cancellationToken);
                }

                var policy = new ShippingPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    HandlingTimeDays = request.HandlingTimeDays,
                    IsDefault = request.IsDefault,

                    OfferFreeShipping = request.OfferFreeShipping,

                    DomesticCostType = request.DomesticCostType,
                    DomesticServicesJson = JsonSerializer.Serialize(request.DomesticServices),

                    IsInternationalShippingAllowed = request.IsInternationalShippingAllowed,
                    InternationalCostType = request.InternationalCostType,
                    InternationalServicesJson = JsonSerializer.Serialize(request.InternationalServices),

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
