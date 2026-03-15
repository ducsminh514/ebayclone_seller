using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Shops
{
    public interface IUpdateShopProfileUseCase
    {
        Task ExecuteAsync(Guid userId, UpdateShopProfileRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// SECURITY: Chỉ cho phép sửa Name, Description, AvatarUrl, BannerUrl.
    /// KHÔNG cho phép sửa IsVerified, MonthlyListingLimit, Bank info qua endpoint này.
    /// PERFORMANCE: Dùng partial update (chỉ update field có giá trị) thay vì load full entity.
    /// </summary>
    public class UpdateShopProfileUseCase : IUpdateShopProfileUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateShopProfileUseCase(IShopRepository shopRepository, IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid userId, UpdateShopProfileRequest request, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            if (shop == null)
            {
                throw new InvalidOperationException("User does not have a shop.");
            }

            // Partial update: chỉ sửa field nào có giá trị được gửi lên
            bool hasChanges = false;

            if (request.Name != null && request.Name != shop.Name)
            {
                // SECURITY: Sanitize shop name - strip HTML tags
                shop.Name = System.Text.RegularExpressions.Regex.Replace(request.Name, "<.*?>", string.Empty).Trim();
                hasChanges = true;
            }

            if (request.Description != null && request.Description != shop.Description)
            {
                shop.Description = System.Text.RegularExpressions.Regex.Replace(request.Description, "<.*?>", string.Empty).Trim();
                hasChanges = true;
            }

            if (request.AvatarUrl != null && request.AvatarUrl != shop.AvatarUrl)
            {
                shop.AvatarUrl = request.AvatarUrl;
                hasChanges = true;
            }

            if (request.BannerUrl != null && request.BannerUrl != shop.BannerUrl)
            {
                shop.BannerUrl = request.BannerUrl;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return; // No changes to save, skip DB round-trip (PERFORMANCE)
            }

            _shopRepository.Update(shop);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
