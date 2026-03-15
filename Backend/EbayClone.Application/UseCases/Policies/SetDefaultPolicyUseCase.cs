using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface ISetDefaultPolicyUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid policyId, string policyType, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SECURITY: Kiểm tra policy thuộc shopId (chống IDOR).
    /// ATOMIC: Clear default cũ → set default mới trong 1 transaction.
    /// </summary>
    public class SetDefaultPolicyUseCase : ISetDefaultPolicyUseCase
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetDefaultPolicyUseCase(IPolicyRepository policyRepository, IUnitOfWork unitOfWork)
        {
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid policyId, string policyType, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                switch (policyType.ToLower())
                {
                    case "shipping":
                        var sp = await _policyRepository.GetShippingPolicyByIdAsync(policyId, cancellationToken);
                        if (sp == null || sp.ShopId != shopId)
                            throw new InvalidOperationException("Shipping policy not found or does not belong to your shop.");
                        await _policyRepository.ClearDefaultShippingPolicyAsync(shopId, cancellationToken);
                        sp.IsDefault = true;
                        _policyRepository.UpdateShippingPolicy(sp);
                        break;

                    case "return":
                        var rp = await _policyRepository.GetReturnPolicyByIdAsync(policyId, cancellationToken);
                        if (rp == null || rp.ShopId != shopId)
                            throw new InvalidOperationException("Return policy not found or does not belong to your shop.");
                        await _policyRepository.ClearDefaultReturnPolicyAsync(shopId, cancellationToken);
                        rp.IsDefault = true;
                        _policyRepository.UpdateReturnPolicy(rp);
                        break;

                    case "payment":
                        var pp = await _policyRepository.GetPaymentPolicyByIdAsync(policyId, cancellationToken);
                        if (pp == null || pp.ShopId != shopId)
                            throw new InvalidOperationException("Payment policy not found or does not belong to your shop.");
                        await _policyRepository.ClearDefaultPaymentPolicyAsync(shopId, cancellationToken);
                        pp.IsDefault = true;
                        _policyRepository.UpdatePaymentPolicy(pp);
                        break;

                    default:
                        throw new ArgumentException($"Unknown policy type: {policyType}");
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
