using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Products;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Products
{
    public interface IRestockVariantUseCase
    {
        // shopId: lấy từ JWT claim, dùng để verify quyền sở hữu variant
        Task<bool> ExecuteAsync(Guid shopId, Guid variantId, RestockVariantRequest request, CancellationToken cancellationToken = default);
    }

    public class RestockVariantUseCase : IRestockVariantUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RestockVariantUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid shopId, Guid variantId, RestockVariantRequest request, CancellationToken cancellationToken = default)
        {
            // Chống IDOR: verify variant thuộc shop của seller đang thao tác
            var variant = await _productRepository.GetVariantByIdAsync(variantId, cancellationToken);
            if (variant == null)
                throw new ArgumentException("Biến thể không tồn tại.");

            var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại hoặc đã bị ẩn.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền nhập kho sản phẩm này.");

            // Thực hiện restock atomic (UPDATE ... SET Quantity = Quantity + X)
            // Không query rồi trừ bằng code → tránh race condition
            int rowsAffected = await _productRepository.RestockVariantAsync(variantId, request.AddedQuantity, cancellationToken);
            
            if (rowsAffected == 0)
                throw new ArgumentException("Không thể nhập kho. Biến thể có thể không tồn tại.");

            // [A6] Auto OUT_OF_STOCK → ACTIVE khi restock
            // Atomic update đã thay đổi DB trực tiếp → cần reload product để có stock mới
            var updatedProduct = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
            if (updatedProduct != null)
            {
                updatedProduct.CheckAndUpdateStockStatus();
                await _productRepository.UpdateAsync(updatedProduct, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}
