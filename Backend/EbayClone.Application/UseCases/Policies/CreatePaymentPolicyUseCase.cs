using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Policies
{
    public interface ICreatePaymentPolicyUseCase
    {
        Task<Guid> ExecuteAsync(Guid shopId, CreatePaymentPolicyRequest request, CancellationToken cancellationToken = default);
    }

    public class CreatePaymentPolicyUseCase : ICreatePaymentPolicyUseCase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePaymentPolicyUseCase(
            IPolicyRepository policyRepository, 
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreatePaymentPolicyRequest request, CancellationToken cancellationToken = default)
        {
            // SECURITY: Serializable isolation chống Race Condition khi đếm count
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            try
            {
                // PERFORMANCE: Count() trực tiếp thay vì kéo full list
                var currentPolicyCount = await _policyRepository.CountPaymentPoliciesAsync(shopId, cancellationToken);
                if (currentPolicyCount >= 100)
                {
                    throw new InvalidOperationException("You have reached the maximum limit of 100 payment policies.");
                }

                var policy = new PaymentPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    ImmediatePaymentRequired = request.ImmediatePaymentRequired,
                    DaysToPayment = request.DaysToPayment,
                    PaymentMethod = request.PaymentMethod,
                    PaymentInstructions = request.PaymentInstructions,
                    IsDefault = request.IsDefault,
                    IsArchived = false
                };

                // Data Integrity: Chỉ cho phép 1 default policy per type
                if (policy.IsDefault)
                {
                    await _policyRepository.ClearDefaultPaymentPolicyAsync(shopId, cancellationToken);
                }

                await _policyRepository.AddPaymentPolicyAsync(policy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                // SCALABILITY: Atomic Update bộ đếm thay vì shop.Total += 1
                await _shopRepository.IncrementTotalPaymentPoliciesAsync(shopId, cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return policy.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to create Payment Policy: {ex.Message}", ex);
            }
        }
    }
}
