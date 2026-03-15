using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Services;

namespace EbayClone.Application.UseCases.Shops
{
    public interface IVerifyMicroDepositUseCase
    {
        Task<bool> ExecuteAsync(Guid userId, VerifyMicroDepositRequest request, CancellationToken cancellationToken = default);
    }

    public class VerifyMicroDepositUseCase : IVerifyMicroDepositUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly IDefaultPolicySeeder _policySeeder;
        private readonly IUnitOfWork _unitOfWork;
 
        public VerifyMicroDepositUseCase(
            IShopRepository shopRepository, 
            IDefaultPolicySeeder policySeeder,
            IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _policySeeder = policySeeder;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid userId, VerifyMicroDepositRequest request, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            if (shop == null)
            {
                throw new InvalidOperationException("Shop not found for this user.");
            }

            if (shop.BankVerificationStatus != "Pending")
            {
                throw new InvalidOperationException("Bank verification is not in Pending state.");
            }

            // 1. Kiểm tra số lần thử (Brute-force protection)
            if (shop.BankVerificationAttempts >= 3)
            {
                throw new InvalidOperationException("Too many failed attempts. Your bank verification is locked. Please contact support.");
            }

            // 2. Kiểm tra so khớp 2 khoản tiền (Chuyển sang cents để so sánh chính xác tuyệt đối)
            int expectedCents1 = (int)Math.Round(shop.MicroDepositAmount1 * 100);
            int expectedCents2 = (int)Math.Round(shop.MicroDepositAmount2 * 100);
            int inputCents1 = (int)Math.Round(request.Amount1 * 100);
            int inputCents2 = (int)Math.Round(request.Amount2 * 100);

            if (expectedCents1 == inputCents1 && expectedCents2 == inputCents2)
            {
                shop.BankVerificationStatus = "Verified";
                shop.BankVerificationAttempts = 0;
                
                if (shop.IsIdentityVerified)
                {
                    shop.IsVerified = true;
                    shop.IsPolicyOptedIn = true; // Auto opt-in khi verified (default policies seeded cùng lúc)
                    // Seed policies only when fully verified
                    await _policySeeder.SeedDefaultPoliciesAsync(shop.Id, cancellationToken);
                }

                _shopRepository.Update(shop);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }

            // 3. Nếu sai: Tăng số lần thử và thông báo lỗi
            shop.BankVerificationAttempts++;
            _shopRepository.Update(shop);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            int remaining = 3 - shop.BankVerificationAttempts;
            throw new InvalidOperationException($"Verification amounts do not match. You have {remaining} attempts remaining.");
        }
    }
}
