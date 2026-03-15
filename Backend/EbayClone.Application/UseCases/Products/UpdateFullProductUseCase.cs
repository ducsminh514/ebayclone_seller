using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Products;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Products
{
    public interface IUpdateFullProductUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid productId, UpdateFullProductRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateFullProductUseCase : IUpdateFullProductUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateFullProductUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, UpdateFullProductRequest request, CancellationToken cancellationToken = default)
        {
            // Load product with variants in a single transaction-ready state
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            
            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sản phẩm này.");

            // 1. Optimistic Concurrency Check
            if (request.RowVersion != null && product.RowVersion != null)
            {
                if (!request.RowVersion.SequenceEqual(product.RowVersion))
                {
                    throw new InvalidOperationException("Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang.");
                }
            }

            // 2. Update Master Data
            product.Name = request.Name;
            product.Description = request.Description;
            product.Brand = request.Brand;
            product.CategoryId = request.CategoryId;
            product.ShippingPolicyId = request.ShippingPolicyId;
            product.ReturnPolicyId = request.ReturnPolicyId;
            product.Status = request.Status;
            
            product.PrimaryImageUrl = request.PrimaryImageUrl;
            product.ImageUrls = request.ImageUrls != null && request.ImageUrls.Any()
                ? JsonSerializer.Serialize(request.ImageUrls)
                : null;
            
            product.UpdatedAt = DateTimeOffset.UtcNow;
            // Audit: LastModifiedBy should be set from identity if available

            // 3. Variant Syncing (Solve Ghost Variants)
            var existingVariants = product.Variants.ToList();
            var incomingVariantIds = request.Variants.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToList();

            // A. Remove variants not in request
            foreach (var existing in existingVariants)
            {
                if (!incomingVariantIds.Contains(existing.Id))
                {
                    product.Variants.Remove(existing);
                }
            }

            // B. Update or Add
            foreach (var variantDto in request.Variants)
            {
                if (variantDto.Id.HasValue)
                {
                    var variant = existingVariants.FirstOrDefault(v => v.Id == variantDto.Id.Value);
                    if (variant != null)
                    {
                        variant.SkuCode = variantDto.SkuCode;
                        variant.Price = variantDto.Price;
                        variant.ImageUrl = variantDto.ImageUrl;
                        variant.Attributes = JsonSerializer.Serialize(variantDto.Attributes);
                        variant.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    var newVariant = new ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        SkuCode = variantDto.SkuCode,
                        Price = variantDto.Price,
                        Quantity = 0, // Enforce restock flow
                        ReservedQuantity = 0,
                        Attributes = JsonSerializer.Serialize(variantDto.Attributes),
                        ImageUrl = variantDto.ImageUrl,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    product.Variants.Add(newVariant);
                }
            }

            try 
            {
                await _productRepository.UpdateAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
            {
                throw new InvalidOperationException("Xung đột dữ liệu xảy ra: Có người vừa cập nhật sản phẩm này. Hãy làm mới trang.");
            }
        }
    }
}
