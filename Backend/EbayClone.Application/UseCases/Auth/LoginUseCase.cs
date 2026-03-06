using EbayClone.Application.DTOs.Auth;
using EbayClone.Application.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Auth
{
    public interface ILoginUseCase
    {
        Task<LoginResultDto> ExecuteAsync(LoginRequest request);
    }

    public class LoginUseCase : ILoginUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IShopRepository _shopRepository;
        
        public LoginUseCase(IUserRepository userRepository, IShopRepository shopRepository)
        {
            _userRepository = userRepository;
            _shopRepository = shopRepository;
        }

        public async Task<LoginResultDto> ExecuteAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            if (!user.IsEmailVerified)
            {
                throw new UnauthorizedAccessException("Email is not verified. Please verify your email first.");
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            var shop = await _shopRepository.GetByUserIdAsync(user.Id);

            return new LoginResultDto
            {
                UserId = user.Id.ToString(),
                Username = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName.Trim() : user.Username,
                HasShop = shop != null,
                ShopId = shop?.Id
            };
        }
    }
}
