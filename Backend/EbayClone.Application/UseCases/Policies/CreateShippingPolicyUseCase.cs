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
            // Bảo vệ Database tuyệt đối chống Race Condition TOCTOU:
            // Khóa phạm vi bằng mức IsolationLevel.Serializable để các request query đếm số lượng sẽ phải xếp hàng
            await _unitOfWork.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

            try
            {
                // Dùng hàm đếm Count trực tiếp thay vì Select nguyên object Shop khổng lồ (giảm IOPS)
                var currentPolicyCount = await _policyRepository.CountShippingPoliciesAsync(shopId, cancellationToken);
                if (currentPolicyCount >= 100)
                {
                    throw new InvalidOperationException("You have reached the maximum limit of 100 shipping policies.");
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
                    HandlingTimeDays = request.HandlingTimeDays,
                    Cost = request.Cost,
                    IsDefault = request.IsDefault
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
