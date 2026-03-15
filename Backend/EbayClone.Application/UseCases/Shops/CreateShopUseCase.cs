using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Shops
{
    public interface ICreateShopUseCase
    {
        Task<Guid> ExecuteAsync(Guid userId, CreateShopRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateShopUseCase : ICreateShopUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateShopUseCase(
            IShopRepository shopRepository, 
            ISellerWalletRepository walletRepository,
            IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid userId, CreateShopRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Validate: Mỗi user chỉ được tạo 1 shop
            var exists = await _shopRepository.ExistsByUserIdAsync(userId, cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException("User already owns a shop.");
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 2. Map Entity (Auto Verified)
                var shop = new Shop
                {
                    OwnerId = userId,
                    Name = request.Name,
                    Description = request.Description,
                    TaxCode = request.TaxCode,
                    Address = request.Address,
                    IsVerified = false, // Tự động duyệt vì không có luồng Admin
                    RatingAvg = 0.0m
                };

                await _shopRepository.AddAsync(shop, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Save để sinh ra shop.Id

                // 3. Khởi tạo ví SellerWallet rỗng
                var wallet = new SellerWallet
                {
                    ShopId = shop.Id
                };
                await _walletRepository.AddAsync(wallet, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return shop.Id;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to create shop and initialize wallet: {ex.Message}", ex);
            }
        }
    }
}
