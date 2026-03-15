using System;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Auth;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Auth
{
    public interface IRefreshTokenUseCase
    {
        Task<LoginResultDto> ExecuteAsync(Guid userId);
    }

    public class RefreshTokenUseCase : IRefreshTokenUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IShopRepository _shopRepository;

        public RefreshTokenUseCase(IUserRepository userRepository, IShopRepository shopRepository)
        {
            _userRepository = userRepository;
            _shopRepository = shopRepository;
        }

        public async Task<LoginResultDto> ExecuteAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var shop = await _shopRepository.GetByUserIdAsync(user.Id);

            return new LoginResultDto
            {
                UserId = user.Id.ToString(),
                Username = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName.Trim() : user.Username,
                Role = user.Role,
                HasShop = shop != null,
                IsVerified = shop?.IsVerified ?? false,
                ShopId = shop?.Id
            };
        }
    }
}
