using EbayClone.Application.DTOs.Auth;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using System;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Auth
{
    public interface IVerifyEmailUseCase
    {
        Task<bool> ExecuteAsync(VerifyEmailRequest request);
    }

    public class VerifyEmailUseCase : IVerifyEmailUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public VerifyEmailUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(VerifyEmailRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            if (user.IsEmailVerified)
            {
                return true; // Already verified
            }

            if (user.EmailVerificationTokenExpiresAt < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("Verification token has expired.");
            }

            if (!string.Equals(user.EmailVerificationToken, request.Token, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid verification token.");
            }

            // Update user status
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
