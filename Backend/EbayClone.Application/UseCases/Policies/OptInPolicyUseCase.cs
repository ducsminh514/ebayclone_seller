using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Policies
{
    public interface IOptInPolicyUseCase
    {
        Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// eBay thật: Seller phải chủ động opt-in vào Business Policies trước khi sử dụng.
    /// Chỉ cho phép opt-in khi Shop đã verified.
    /// </summary>
    public class OptInPolicyUseCase : IOptInPolicyUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OptInPolicyUseCase(IShopRepository shopRepository, IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            if (shop == null)
                throw new InvalidOperationException("User does not have a shop.");

            if (!shop.IsVerified)
                throw new InvalidOperationException("Shop must be fully verified before opting into business policies.");

            if (shop.IsPolicyOptedIn)
                return; // Already opted in, idempotent

            shop.IsPolicyOptedIn = true;
            _shopRepository.Update(shop);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
