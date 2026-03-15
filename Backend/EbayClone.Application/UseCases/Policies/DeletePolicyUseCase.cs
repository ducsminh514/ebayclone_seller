using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface IDeletePolicyUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid policyId, string policyType, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SECURITY: Kiểm tra policy thuộc shopId trước khi xóa (chống IDOR).
    /// Không cho xóa default policy nếu chỉ còn 1 policy.
    /// </summary>
    public class DeletePolicyUseCase : IDeletePolicyUseCase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePolicyUseCase(IPolicyRepository policyRepository, IShopRepository shopRepository, IUnitOfWork unitOfWork)
        {
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid policyId, string policyType, CancellationToken cancellationToken = default)
        {
            switch (policyType.ToLower())
            {
                case "shipping":
                    var sp = await _policyRepository.GetShippingPolicyByIdAsync(policyId, cancellationToken);
                    if (sp == null || sp.ShopId != shopId)
                        throw new InvalidOperationException("Shipping policy not found or does not belong to your shop.");
                    await _policyRepository.DeleteShippingPolicyAsync(sp, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _shopRepository.DecrementTotalShippingPoliciesAsync(shopId, cancellationToken);
                    break;

                case "return":
                    var rp = await _policyRepository.GetReturnPolicyByIdAsync(policyId, cancellationToken);
                    if (rp == null || rp.ShopId != shopId)
                        throw new InvalidOperationException("Return policy not found or does not belong to your shop.");
                    await _policyRepository.DeleteReturnPolicyAsync(rp, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _shopRepository.DecrementTotalReturnPoliciesAsync(shopId, cancellationToken);
                    break;

                case "payment":
                    var pp = await _policyRepository.GetPaymentPolicyByIdAsync(policyId, cancellationToken);
                    if (pp == null || pp.ShopId != shopId)
                        throw new InvalidOperationException("Payment policy not found or does not belong to your shop.");
                    await _policyRepository.DeletePaymentPolicyAsync(pp, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _shopRepository.DecrementTotalPaymentPoliciesAsync(shopId, cancellationToken);
                    break;

                default:
                    throw new ArgumentException($"Unknown policy type: {policyType}");
            }
        }
    }
}
