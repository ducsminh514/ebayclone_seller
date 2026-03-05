using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Policies;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

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
            // Lấy trực tiếp Shop (Không tracking if want, but we need to update it here)
            // ShopRepository GetByIdAsync hiện đang Tracking mặc định nên ta có thể Update()
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null)
                throw new ArgumentException("Shop not found");

            // Bảo vệ Database: Đọc thẳng từ Cache O(1)
            if (shop.TotalShippingPolicies >= 100)
            {
                throw new InvalidOperationException("You have reached the maximum limit of 100 shipping policies.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var policy = new ShippingPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    HandlingTimeDays = request.HandlingTimeDays,
                    Cost = request.Cost,
                    IsDefault = request.IsDefault
                };

                await _policyRepository.AddShippingPolicyAsync(policy, cancellationToken);
                
                // Tăng bộ đếm Cache
                shop.TotalShippingPolicies += 1;
                _shopRepository.Update(shop);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
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
