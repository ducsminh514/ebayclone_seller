using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Products
{
    public interface ISoftDeleteProductUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid productId, CancellationToken cancellationToken = default);
    }

    public class SoftDeleteProductUseCase : ISoftDeleteProductUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SoftDeleteProductUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại hoặc đã bị xóa.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa sản phẩm này.");

            // Soft Delete: đánh dấu IsDeleted, không xóa vật lý
            product.IsDeleted = true;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
