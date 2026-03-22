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
                // Whitelist validation — eBay chỉ cho phép 14, 30, 60 days
                var allowedDays = new HashSet<int> { 14, 30, 60 };
                var allowedRefundMethods = new HashSet<string> { "MoneyBack", "MoneyBackOrReplacement", "MoneyBackOrExchange" };

                if (request.IsDomesticAccepted && !allowedDays.Contains(request.DomesticReturnDays))
                    throw new InvalidOperationException("DomesticReturnDays must be 14, 30, or 60.");
                if (request.IsInternationalAccepted && !allowedDays.Contains(request.InternationalReturnDays))
                    throw new InvalidOperationException("InternationalReturnDays must be 14, 30, or 60.");
                if (request.IsDomesticAccepted && !allowedRefundMethods.Contains(request.DomesticRefundMethod))
                    throw new InvalidOperationException("Invalid DomesticRefundMethod.");
                if (request.IsInternationalAccepted && !allowedRefundMethods.Contains(request.InternationalRefundMethod))
                    throw new InvalidOperationException("Invalid InternationalRefundMethod.");

                // Conditional clear: khi Accept=false → reset fields về default
                // Tránh lưu data vô nghĩa vào DB
                var domesticDays = request.IsDomesticAccepted ? request.DomesticReturnDays : 30;
                var domesticPaidBy = request.IsDomesticAccepted ? request.DomesticShippingPaidBy : "BUYER";
                var domesticRefund = request.IsDomesticAccepted ? request.DomesticRefundMethod : "MoneyBack";
                var intlDays = request.IsInternationalAccepted ? request.InternationalReturnDays : 30;
                var intlPaidBy = request.IsInternationalAccepted ? request.InternationalShippingPaidBy : "BUYER";
                var intlRefund = request.IsInternationalAccepted ? request.InternationalRefundMethod : "MoneyBack";

                var policy = new ReturnPolicy
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    
                    IsDomesticAccepted = request.IsDomesticAccepted,
                    DomesticReturnDays = domesticDays,
                    DomesticShippingPaidBy = domesticPaidBy,
                    DomesticRefundMethod = domesticRefund,

                    IsInternationalAccepted = request.IsInternationalAccepted,
                    InternationalReturnDays = intlDays,
                    InternationalShippingPaidBy = intlPaidBy,
                    InternationalRefundMethod = intlRefund,

                    AutoAcceptReturns = request.AutoAcceptReturns,
                    SendImmediateRefund = request.SendImmediateRefund,
                    ReturnAddressJson = request.ReturnAddressJson,
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
