using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Policies;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Policies
{
    public interface ICreateReturnPolicyUseCase
    {
        Task<Guid> ExecuteAsync(Guid shopId, CreateReturnPolicyRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateReturnPolicyUseCase : ICreateReturnPolicyUseCase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateReturnPolicyUseCase(
            IPolicyRepository policyRepository, 
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreateReturnPolicyRequest request, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null)
                throw new ArgumentException("Shop not found");

            // Đọc thẳng từ Cache O(1)
            if (shop.TotalReturnPolicies >= 100)
            {
                throw new InvalidOperationException("You have reached the maximum limit of 100 return policies.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var policy = new ReturnPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    ReturnDays = request.ReturnDays,
                    ShippingPaidBy = request.ShippingPaidBy
                };

                await _policyRepository.AddReturnPolicyAsync(policy, cancellationToken);
                
                // Tăng Cache
                shop.TotalReturnPolicies += 1;
                _shopRepository.Update(shop);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return policy.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to create Return Policy: {ex.Message}", ex);
            }
        }
    }
}
