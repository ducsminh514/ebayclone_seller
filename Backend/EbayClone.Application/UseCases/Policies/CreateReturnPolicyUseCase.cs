using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
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
            // Bịt lỗ hổng TOCTOU: Giam lỏng toàn bộ request tạo Cùng một lúc vào hàng đợi Serializable
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            try
            {
                // Dùng thủ thuật đ Count() trực tiếp tối ưu IOPS thay vì kéo Full Shop
                var currentPolicyCount = await _policyRepository.CountReturnPoliciesAsync(shopId, cancellationToken);
                if (currentPolicyCount >= 100)
                {
                    throw new InvalidOperationException("You have reached the maximum limit of 100 return policies.");
                }
                var policy = new ReturnPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    
                    IsDomesticAccepted = request.IsDomesticAccepted,
                    DomesticReturnDays = request.DomesticReturnDays,
                    DomesticShippingPaidBy = request.DomesticShippingPaidBy,

                    IsInternationalAccepted = request.IsInternationalAccepted,
                    InternationalReturnDays = request.InternationalReturnDays,
                    InternationalShippingPaidBy = request.InternationalShippingPaidBy,

                    AutoAcceptReturns = request.AutoAcceptReturns,
                    SendImmediateRefund = request.SendImmediateRefund,
                    ReturnAddressJson = request.ReturnAddressJson,
                    RestockingFeePercent = request.RestockingFeePercent,
                    
                    IsDefault = request.IsDefault,
                    IsArchived = false
                };

                if (policy.IsDefault)
                {
                    await _policyRepository.ClearDefaultReturnPolicyAsync(shopId, cancellationToken);
                }

                await _policyRepository.AddReturnPolicyAsync(policy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                // Tăng Cache bằng SQL Atomic Update
                await _shopRepository.IncrementTotalReturnPoliciesAsync(shopId, cancellationToken);

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
