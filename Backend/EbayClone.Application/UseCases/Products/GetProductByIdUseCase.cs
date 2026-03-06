using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Products
{
    public interface IGetProductByIdUseCase
    {
        Task<Product?> ExecuteAsync(Guid shopId, Guid productId, CancellationToken cancellationToken = default);
    }

    public class GetProductByIdUseCase : IGetProductByIdUseCase
    {
        private readonly IProductRepository _productRepository;

        public GetProductByIdUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Product?> ExecuteAsync(Guid shopId, Guid productId, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product != null && product.ShopId != shopId)
            {
                // Simple authorization check
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập sản phẩm này.");
            }
            return product;
        }
    }
}
