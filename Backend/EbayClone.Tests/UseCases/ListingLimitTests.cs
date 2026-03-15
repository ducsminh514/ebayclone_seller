using System;
using System.Collections.Generic;
using Moq;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.Interfaces;
using EbayClone.Application.UseCases.Products;
using EbayClone.Shared.DTOs.Products;
using EbayClone.Domain.Entities;
using Xunit;

namespace EbayClone.Tests.UseCases
{
    public class ListingLimitTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<IShopRepository> _shopRepositoryMock;
        private readonly Mock<IPolicyRepository> _policyRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        public ListingLimitTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _shopRepositoryMock = new Mock<IShopRepository>();
            _policyRepositoryMock = new Mock<IPolicyRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
        }

        [Fact]
        public async Task CreateListing_WhenLimitReached_ShouldThrowException()
        {
            // Arrange
            var shopId = Guid.NewGuid();
            var shop = new Shop { Id = shopId, MonthlyListingLimit = 250 };
            
            _shopRepositoryMock.Setup(r => r.GetByIdAsync(shopId, It.IsAny<CancellationToken>())).ReturnsAsync(shop);
            _productRepositoryMock.Setup(r => r.GetCountByShopInCurrentMonthAsync(shopId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(10); // Limit reached

            var useCase = new CreateListingUseCase(
                _productRepositoryMock.Object,
                _shopRepositoryMock.Object,
                _policyRepositoryMock.Object,
                _unitOfWorkMock.Object);

            var request = new CreateListingRequest 
            { 
                Name = "Test Product",
                Variants = new List<CreateVariantRequest> { new CreateVariantRequest { SkuCode = "V1", Quantity = 1 } }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                useCase.ExecuteAsync(shopId, request));
            
            Assert.Contains("Shop đã đạt giới hạn niêm yết", exception.Message);
        }
    }
}
