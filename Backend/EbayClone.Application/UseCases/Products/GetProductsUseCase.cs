using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Products
{
    public interface IGetProductsUseCase
    {
        Task<IEnumerable<Product>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetProductsUseCase : IGetProductsUseCase
    {
        private readonly IProductRepository _productRepository;

        public GetProductsUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Product>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _productRepository.GetProductsByShopIdAsync(shopId, cancellationToken);
        }
    }
}
