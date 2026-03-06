using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Products
{
    public interface IRestockVariantUseCase
    {
        Task<bool> ExecuteAsync(Guid variantId, RestockVariantRequest request, CancellationToken cancellationToken = default);
    }

    public class RestockVariantUseCase : IRestockVariantUseCase
    {
        private readonly IProductRepository _productRepository;

        public RestockVariantUseCase(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<bool> ExecuteAsync(Guid variantId, RestockVariantRequest request, CancellationToken cancellationToken = default)
        {
            // Bảo vệ bằng ExecuteUpdateAsync: 
            // Query này bắn thẳng lệnh SET Quantity = Quantity + X xuống hệ quản trị SQL, 
            // bỏ qua bộ nhớ đệm Tracking của RAM để chống Overselling (Bán ảo) 100%.
            int rowsAffected = await _productRepository.RestockVariantAsync(variantId, request.AddedQuantity, cancellationToken);
            
            if (rowsAffected == 0)
            {
                throw new ArgumentException("Variant not found or unable to restock.");
            }

            return true;
        }
    }
}
