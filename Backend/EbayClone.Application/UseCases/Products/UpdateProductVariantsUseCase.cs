using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Products
{
    public interface IUpdateProductVariantsUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductVariantsRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateProductVariantsUseCase : IUpdateProductVariantsUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductVariantsUseCase(IProductRepository productRepository, IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductVariantsRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại hoặc đã bị ẩn.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sản phẩm này.");

            // Load existing variants
            var existingVariants = product.Variants?.ToList() ?? new List<ProductVariant>();

            foreach (var variantDto in request.Variants)
            {
                if (variantDto.Id.HasValue)
                {
                    // Update existing
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
                    // Optionally: Add new variant if Id is null
                    // Note: Quantity defaults to 0 as they must restock later
                    var newVariant = new ProductVariant
                    {
                        Id = Guid.NewGuid(),
                        ProductId = productId,
                        SkuCode = variantDto.SkuCode,
                        Price = variantDto.Price,
                        Quantity = 0,
                        ReservedQuantity = 0,
                        Attributes = JsonSerializer.Serialize(variantDto.Attributes),
                        ImageUrl = variantDto.ImageUrl,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    product.Variants?.Add(newVariant);
                }
            }

            // Note on Performance: We use regular Repository.Update here because we are modifying a complex collection.
            // EF Core tracking handles identifying what changed.
            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
