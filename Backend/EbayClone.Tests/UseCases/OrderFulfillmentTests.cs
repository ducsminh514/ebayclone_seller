using EbayClone.Application.UseCases.Orders;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.Interfaces;
using EbayClone.Domain.Entities;
using Moq;
using Xunit;
using EbayClone.Shared.DTOs.Orders;

namespace EbayClone.Tests.UseCases
{
    public class OrderFulfillmentTests
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IShopRepository> _shopRepositoryMock;
        private readonly Mock<ISellerWalletRepository> _walletRepositoryMock;
        private readonly Mock<IWalletTransactionRepository> _walletTransactionRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        public OrderFulfillmentTests()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _productRepositoryMock = new Mock<IProductRepository>();
            _shopRepositoryMock = new Mock<IShopRepository>();
            _walletRepositoryMock = new Mock<ISellerWalletRepository>();
            _walletTransactionRepositoryMock = new Mock<IWalletTransactionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
        }

        [Fact]
        public async Task CreateOrder_ShouldBeIdempotent()
        {
            // Arrange
            var idempotencyKey = "unique-key-123";
            var buyerId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var existingOrder = new Order { Id = Guid.NewGuid(), IdempotencyKey = idempotencyKey };

            _orderRepositoryMock.Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingOrder);

            var useCase = new CreateTestOrderUseCase(
                _productRepositoryMock.Object,
                _orderRepositoryMock.Object,
                _unitOfWorkMock.Object);

            var request = new CreateBuyerTestOrderRequest 
            { 
                IdempotencyKey = idempotencyKey,
                VariantId = variantId,
                Quantity = 1
            };

            // Act
            var resultId = await useCase.ExecuteAsync(buyerId, request);

            // Assert
            Assert.Equal(existingOrder.Id, resultId);
            _orderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusToPaid_ShouldAddMoneyToPendingBalance()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var shopId = Guid.NewGuid();
            var order = new Order 
            { 
                Id = orderId, 
                ShopId = shopId, 
                TotalAmount = 100000 
            };
            // Mock RowVersion match
            var rowVersion = new byte[] { 1, 2, 3 };
            typeof(Order).GetProperty("RowVersion")?.SetValue(order, rowVersion);

            var wallet = new SellerWallet { ShopId = shopId };

            _orderRepositoryMock.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
            _walletRepositoryMock.Setup(r => r.GetByShopIdAsync(shopId, It.IsAny<CancellationToken>())).ReturnsAsync(wallet);

            var useCase = new UpdateOrderStatusUseCase(
                _orderRepositoryMock.Object,
                _walletRepositoryMock.Object,
                _walletTransactionRepositoryMock.Object,
                _productRepositoryMock.Object,
                _unitOfWorkMock.Object);

            // Act: Chuyển sang PAID (eBay 2024 flow)
            await useCase.ExecuteAsync(shopId, orderId, new UpdateOrderStatusRequest 
            { 
                NewStatus = "PAID", 
                RowVersion = rowVersion 
            });

            // Assert
            Assert.Equal(100000, wallet.PendingBalance); // Cả cục TotalAmount vào Pending theo chuẩn eBay 2024
            Assert.Equal("PAID", order.PaymentStatus);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
