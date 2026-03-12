using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Finance;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Finance;
using EbayClone.Domain.Entities;
using Moq;
using Xunit;

namespace EbayClone.Tests.UseCases
{
    public class FinanceModuleTests
    {
        private readonly Mock<IShopRepository> _shopRepositoryMock;
        private readonly Mock<ISellerWalletRepository> _walletRepositoryMock;
        private readonly Mock<IWalletTransactionRepository> _transactionRepositoryMock;

        public FinanceModuleTests()
        {
            _shopRepositoryMock = new Mock<IShopRepository>();
            _walletRepositoryMock = new Mock<ISellerWalletRepository>();
            _transactionRepositoryMock = new Mock<IWalletTransactionRepository>();
        }

        [Fact]
        public async Task GetSellerFinance_ShouldReturnCorrectData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var shopId = Guid.NewGuid();
            var shop = new Shop { Id = shopId, OwnerId = userId };
            
            var wallet = new SellerWallet { ShopId = shopId };
            wallet.AddPending(500);
            wallet.ReleaseEscrow(200); // 200 available, 300 pending

            var transactions = new List<WalletTransaction>
            {
                new WalletTransaction 
                { 
                    Id = Guid.NewGuid(), 
                    ShopId = shopId, 
                    Amount = 100, 
                    Type = "ORDER_INCOME", 
                    Status = "COMPLETED",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            _shopRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shop);
            _walletRepositoryMock.Setup(r => r.GetByShopIdAsync(shopId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(wallet);
            _transactionRepositoryMock.Setup(r => r.GetByWalletIdAsync(wallet.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactions);

            var useCase = new GetSellerFinanceUseCase(
                _shopRepositoryMock.Object,
                _walletRepositoryMock.Object, 
                _transactionRepositoryMock.Object);

            // Act
            var result = await useCase.ExecuteAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.AvailableBalance);
            Assert.Equal(300, result.PendingBalance);
            Assert.Single(result.RecentTransactions);
            Assert.Equal(100, result.RecentTransactions.First().Amount);
        }

        [Fact]
        public void SellerWallet_ReleaseEscrow_ShouldUpdateBalances()
        {
            // Arrange
            var wallet = new SellerWallet { ShopId = Guid.NewGuid() };
            wallet.AddPending(500);

            // Act
            wallet.ReleaseEscrow(200);

            // Assert
            Assert.Equal(200, wallet.AvailableBalance);
            Assert.Equal(300, wallet.PendingBalance);
        }

        [Fact]
        public void SellerWallet_ReleaseEscrow_WhenInsufficientPending_ShouldThrow()
        {
            // Arrange
            var wallet = new SellerWallet { ShopId = Guid.NewGuid() };
            wallet.AddPending(100);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => wallet.ReleaseEscrow(200));
        }
    }
}
