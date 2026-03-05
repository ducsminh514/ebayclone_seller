using EbayClone.Application.DTOs.Auth;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Auth
{
    public interface IRegisterUserUseCase
    {
        Task<Guid> ExecuteAsync(RegisterRequest request);
    }

    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public RegisterUserUseCase(IUserRepository userRepository, IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
        }

        public async Task<Guid> ExecuteAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                if (existingUser.IsEmailVerified)
                {
                    throw new InvalidOperationException("Email is already registered and verified.");
                }

                // Cập nhật lại User chưa verify (Gửi lại OTP/Thay đổi password)
                existingUser.FullName = $"{request.FirstName} {request.LastName}";
                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                existingUser.EmailVerificationToken = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                existingUser.EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    await _userRepository.UpdateAsync(existingUser);
                    await _unitOfWork.SaveChangesAsync();
                    await _emailService.SendVerificationEmailAsync(existingUser.Email, existingUser.EmailVerificationToken);
                    await _unitOfWork.CommitTransactionAsync();
                    return existingUser.Id;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            string verificationToken = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var user = new User
            {
                Username = request.Email,
                Email = request.Email,
                FullName = $"{request.FirstName} {request.LastName}",
                PasswordHash = passwordHash,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _userRepository.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                
                await _emailService.SendVerificationEmailAsync(user.Email, verificationToken);

                await _unitOfWork.CommitTransactionAsync();
                return user.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
