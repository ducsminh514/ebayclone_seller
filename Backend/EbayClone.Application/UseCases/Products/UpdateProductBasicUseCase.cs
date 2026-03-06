using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Products
{
    public interface IUpdateProductBasicUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductBasicRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateProductBasicUseCase : IUpdateProductBasicUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductBasicUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductBasicRequest request, CancellationToken cancellationToken = default)
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

            product.Name = request.Name;
            product.Description = request.Description;
            product.Brand = request.Brand;
            product.CategoryId = request.CategoryId;
            product.ShippingPolicyId = request.ShippingPolicyId;
            product.ReturnPolicyId = request.ReturnPolicyId;
            product.Status = request.Status;
            
            // Xử lý ảnh
            string? primaryImg = request.PrimaryImageUrl;
            if (string.IsNullOrEmpty(primaryImg) && request.ImageUrls != null && request.ImageUrls.Any())
                primaryImg = request.ImageUrls[0];
            
            product.PrimaryImageUrl = primaryImg;
            product.ImageUrls = request.ImageUrls != null && request.ImageUrls.Any()
                ? JsonSerializer.Serialize(request.ImageUrls)
                : null;
            
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
