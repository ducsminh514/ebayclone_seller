using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Products
{
    public interface IUpdateProductStatusUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateProductStatusUseCase : IUpdateProductStatusUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductStatusUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            
            if (product == null)
            {
                throw new ArgumentException("Sản phẩm không tồn tại.");
            }
            
            if (product.ShopId != shopId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sản phẩm này.");
            }

            // Simple validation for allowed statuses
            var allowedStatuses = new[] { "DRAFT", "ACTIVE", "SCHEDULED", "HIDDEN" };
            if (!System.Linq.Enumerable.Contains(allowedStatuses, request.Status.ToUpper()))
            {
                throw new ArgumentException("Trạng thái không hợp lệ.");
            }

            product.Status = request.Status.ToUpper();
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
