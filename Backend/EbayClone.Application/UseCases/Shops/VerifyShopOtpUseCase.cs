using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Services;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Shops
{
    public interface IVerifyShopOtpUseCase
    {
        Task<bool> ExecuteAsync(Guid userId, VerifyShopOtpRequest request, CancellationToken cancellationToken = default);
    }

    public class VerifyShopOtpUseCase : IVerifyShopOtpUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;
 
        // Mock OTP cố định để làm MVP
        private const string MOCK_OTP = "123456";
 
        public VerifyShopOtpUseCase(
            IShopRepository shopRepository,
            ISellerWalletRepository walletRepository,
            IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid userId, VerifyShopOtpRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra Shop có tồn tại và đang chờ duyệt không
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            
            if (shop == null)
            {
                throw new InvalidOperationException("User does not have a shop.");
            }

            if (shop.IsVerified)
            {
                throw new InvalidOperationException("Shop is already verified.");
            }

            // 2. So khớp OTP (MVP Dùng mã 123456)
            if (request.OtpCode != MOCK_OTP)
            {
                throw new InvalidOperationException("Invalid OTP Code.");
            }

            // 3. THỰC THI ATOMIC TRANSACTION CHO TÀI CHÍNH
            // Vừa update Shop, vừa tạo Ví -> phải đảm bảo đồng nhất
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Cập nhật trạng thái Identity
                shop.IsIdentityVerified = true;
                shop.MonthlyListingLimit = 250; // eBay free tier: ~250 sản phẩm/tháng

                _shopRepository.Update(shop);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
 
                // Khởi tạo ví rỗng cho Shop (chỉ khi chưa có)
                var existingWallet = await _walletRepository.GetByShopIdAsync(shop.Id, cancellationToken);
                if (existingWallet == null)
                {
                    var wallet = new SellerWallet
                    {
                        ShopId = shop.Id
                    };
                    await _walletRepository.AddAsync(wallet, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
 
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
 
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to verify shop and initialize wallet: {ex.Message}", ex);
            }
        }
    }
}
