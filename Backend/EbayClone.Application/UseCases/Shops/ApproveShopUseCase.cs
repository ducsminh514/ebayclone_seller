using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Shops
{
    public interface IApproveShopUseCase
    {
        Task<bool> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class ApproveShopUseCase : IApproveShopUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ApproveShopUseCase(
            IShopRepository shopRepository, 
            ISellerWalletRepository walletRepository, 
            IUnitOfWork unitOfWork)
        {
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null)
            {
                throw new ArgumentException("Shop not found.");
            }

            if (shop.IsVerified)
            {
                // Shop đã được duyệt rồi, không làm gì cả
                return true; 
            }

            // Bắt đầu THỰC THI ATOMIC TRANSACTION
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Bước 1: Duyệt Shop
                shop.IsVerified = true;
                _shopRepository.Update(shop);

                // Bước 2: Khởi tạo ví rỗng cho Seller
                var wallet = new SellerWallet
                {
                    ShopId = shopId
                };
                await _walletRepository.AddAsync(wallet, cancellationToken);

                // Lưu lại và Commit
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                // Rollback nếu có bất cứ lỗi nào khi tạo Ví hoặc Update Shop
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to approve shop and initialize wallet: {ex.Message}", ex);
            }
        }
    }
}
