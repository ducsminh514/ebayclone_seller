using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.Interfaces;

namespace EbayClone.Application.UseCases.Shops
{
    public interface ILinkBankAccountUseCase
    {
        Task ExecuteAsync(Guid userId, LinkBankAccountRequest request, CancellationToken cancellationToken = default);
    }

    public class LinkBankAccountUseCase : ILinkBankAccountUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LinkBankAccountUseCase(IShopRepository shopRepository, IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid userId, LinkBankAccountRequest request, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            if (shop == null)
            {
                throw new InvalidOperationException("Shop not found for this user.");
            }

            // eBay quy định: Phải xác thực danh tính trước khi liên kết ngân hàng
            if (!shop.IsIdentityVerified && !shop.IsVerified) // Check IsVerified for backward compatibility
            {
                throw new InvalidOperationException("Please verify your identity before linking a bank account.");
            }

            shop.BankName = request.BankName;
            shop.BankAccountNumber = request.BankAccountNumber;
            shop.BankAccountHolderName = request.BankAccountHolderName;
            shop.BankVerificationStatus = "Pending";

            // Giả lập sinh 2 khoản tiền lẻ ngẫu nhiên (Micro-deposits)
            // Trong thực tế, đây là lệnh gửi sang hệ thống thanh toán
            shop.MicroDepositAmount1 = (decimal)(Random.Shared.Next(1, 99) / 100.0);
            shop.MicroDepositAmount2 = (decimal)(Random.Shared.Next(1, 99) / 100.0);

            _shopRepository.Update(shop);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
