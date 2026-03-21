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

            // [C2] Block edit sản phẩm ENDED — final status, không cho phép sửa
            if (product.Status == "ENDED")
                throw new InvalidOperationException("Không thể chỉnh sửa sản phẩm đã kết thúc (ENDED). Hãy tạo listing mới.");

            // 1. Optimistic Concurrency Check
            if (request.RowVersion != null && product.RowVersion != null)
            {
                if (!request.RowVersion.SequenceEqual(product.RowVersion))
                {
                    throw new InvalidOperationException("Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang.");
                }
            }

            // [C1] Validate Title length (3-80 ký tự — chuẩn eBay Cassini)
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 80)
                throw new ArgumentException("Tiêu đề sản phẩm (Title) phải từ 3 đến 80 ký tự (chuẩn eBay).");

            // [C1] Validate Subtitle length (max 55 ký tự — eBay charge phí)
            if (!string.IsNullOrEmpty(request.Subtitle) && request.Subtitle.Length > 55)
                throw new ArgumentException("Phụ đề (Subtitle) không được vượt quá 55 ký tự (chuẩn eBay).");

            // [C2] Validate Condition whitelist (chuẩn eBay 2024-2025)
            var validConditions = new[]
            {
                "New", "New with tags", "New without tags", "New with imperfections", "Open Box",
                "Certified Refurbished", "Excellent Refurbished", "Very Good Refurbished",
                "Good Refurbished", "Seller Refurbished",
                "Used", "Pre-owned - Excellent", "Pre-owned - Good", "Pre-owned - Fair",
                "For Parts or Not Working"
            };
            if (!validConditions.Contains(request.Condition))
                throw new ArgumentException($"Condition không hợp lệ. Giá trị cho phép: {string.Join(", ", validConditions)}");

            // [A3] Validate ListingFormat + Variations rule khi update
            var validFormats = new[] { "FIXED_PRICE", "AUCTION" };
            if (!validFormats.Contains(request.ListingFormat))
                throw new ArgumentException("ListingFormat phải là FIXED_PRICE hoặc AUCTION.");

            // Tính tổng variants sau update: existing giữ lại + incoming mới
            var incomingNewVariants = request.Variants.Where(v => !v.Id.HasValue).Count();
            var existingKept = request.Variants.Where(v => v.Id.HasValue).Count();
            var totalVariantsAfterUpdate = existingKept + incomingNewVariants;

            if (request.ListingFormat == "AUCTION" && totalVariantsAfterUpdate > 1)
                throw new ArgumentException("Listing dạng AUCTION không hỗ trợ nhiều variations.");

            // [A4-FIX] Variation listing PHẢI dùng Fixed Price
            if (request.IsVariationListing && request.ListingFormat == "AUCTION")
                throw new ArgumentException("Variation listings must use Fixed Price format.");

            // [A4-FIX] RequireImmediatePayment chỉ áp dụng cho Fixed Price
            if (request.RequireImmediatePayment && request.ListingFormat == "AUCTION")
                request.RequireImmediatePayment = false;

            if (request.AutoAcceptPrice.HasValue && request.AutoDeclinePrice.HasValue
                && request.AutoAcceptPrice <= request.AutoDeclinePrice)
                throw new ArgumentException("AutoAcceptPrice phải lớn hơn AutoDeclinePrice.");

            // [A4] Validate Variation Limits
            if (totalVariantsAfterUpdate > 250)
                throw new ArgumentException($"Một listing không được vượt quá 250 biến thể (hiện tại sẽ có {totalVariantsAfterUpdate}).");

            // [A4] Validate max 5 attrs per variant + max 50 options per attribute
            var allRequestAttrs = request.Variants
                .Where(v => v.Attributes != null)
                .SelectMany(v => v.Attributes)
                .GroupBy(kv => kv.Key)
                .ToList();

            foreach (var variantDto in request.Variants)
            {
                if (variantDto.Attributes != null && variantDto.Attributes.Count > 5)
                    throw new ArgumentException($"Biến thể {variantDto.SkuCode} vượt quá giới hạn 5 thuộc tính.");
            }

            foreach (var attrGroup in allRequestAttrs)
            {
                var distinctOptions = attrGroup.Select(kv => kv.Value).Distinct().Count();
                if (distinctOptions > 50)
                    throw new ArgumentException(
                        $"Thuộc tính '{attrGroup.Key}' có {distinctOptions} options, vượt quá giới hạn 50.");
            }

            // [FIX-1] Validate SkuCode unique per listing
            var allSkuCodes = request.Variants.Select(v => v.SkuCode).ToList();
            var duplicateSkus = allSkuCodes.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateSkus.Any())
                throw new ArgumentException($"SkuCode bị trùng trong cùng listing: {string.Join(", ", duplicateSkus)}. Mỗi biến thể phải có SKU riêng biệt.");

            // [FIX-2] Validate duplicate attribute keys per variant
            foreach (var vReq in request.Variants)
            {
                if (vReq.Attributes != null)
                {
                    var duplicateKeys = vReq.Attributes.Keys
                        .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicateKeys.Any())
                        throw new ArgumentException($"Biến thể {vReq.SkuCode} có thuộc tính trùng tên: {string.Join(", ", duplicateKeys)}.");
                }
            }

            // 2. Update Master Data
            product.Name = request.Name;
            product.Description = request.Description;
            product.Brand = request.Brand;
            product.Condition = request.Condition;                     // [A2]
            product.ConditionDescription = request.ConditionDescription; // [A2]
            product.ListingFormat = request.ListingFormat;             // [A3]
            product.AllowOffers = request.AllowOffers;                 // [A3]
            product.AutoAcceptPrice = request.AutoAcceptPrice;         // [A3]
            product.AutoDeclinePrice = request.AutoDeclinePrice;       // [A3]
            product.Subtitle = request.Subtitle;                       // [A3]
            product.RequireImmediatePayment = request.RequireImmediatePayment; // [A4]
            product.IsVariationListing = request.IsVariationListing;   // [A4]
            product.CountryOfOrigin = request.CountryOfOrigin?.ToUpperInvariant(); // [SHIPPING]
            product.PackageLengthCm = request.PackageLengthCm;         // [SHIPPING]
            product.PackageWidthCm = request.PackageWidthCm;           // [SHIPPING]
            product.PackageHeightCm = request.PackageHeightCm;         // [SHIPPING]
            product.ScheduledAt = request.ScheduledAt;                 // Schedule
            product.CategoryId = request.CategoryId;
            product.ShippingPolicyId = request.ShippingPolicyId;
            product.ReturnPolicyId = request.ReturnPolicyId;
            product.PaymentPolicyId = request.PaymentPolicyId;
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
                        variant.Quantity = variantDto.Quantity;
                        variant.WeightGram = variantDto.WeightGram;
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
                        Quantity = variantDto.Quantity,
                        Attributes = JsonSerializer.Serialize(variantDto.Attributes),
                        ImageUrl = variantDto.ImageUrl,
                        WeightGram = variantDto.WeightGram,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    product.Variants.Add(newVariant);
                }
            }

            // [FIX-3] Sync VariantAttributeValues relational store
            // Delete ALL existing attribute values for this product's variants
            await _productRepository.DeleteVariantAttributeValuesByProductIdAsync(productId, cancellationToken);
            // Recreate from request data (đảm bảo JSON ↔ relational luôn đồng bộ)
            var attrValues = new List<VariantAttributeValue>();
            foreach (var variant in product.Variants)
            {
                var matchingDto = request.Variants.FirstOrDefault(v => v.Id == variant.Id || (!v.Id.HasValue && v.SkuCode == variant.SkuCode));
                if (matchingDto?.Attributes == null || !matchingDto.Attributes.Any()) continue;
                foreach (var kv in matchingDto.Attributes)
                {
                    attrValues.Add(new VariantAttributeValue
                    {
                        VariantId = variant.Id,
                        AttributeName = kv.Key,
                        AttributeValue = kv.Value
                    });
                }
            }
            if (attrValues.Count > 0)
            {
                await _productRepository.AddVariantAttributeValuesAsync(attrValues, cancellationToken);
            }

            // [A5] Sync Item Specifics — delete cũ, tạo mới
            if (request.ItemSpecifics != null)
            {
                await _productRepository.DeleteProductItemSpecificsByProductIdAsync(productId, cancellationToken);
                if (request.ItemSpecifics.Count > 0)
                {
                    var specifics = request.ItemSpecifics.Select(s => new ProductItemSpecific
                    {
                        ProductId = productId,
                        Name = s.Name,
                        Value = s.Value
                    }).ToList();
                    await _productRepository.AddProductItemSpecificsAsync(specifics, cancellationToken);
                }
            }

            // [C3] Auto-toggle ACTIVE↔OUT_OF_STOCK khi Quantity thay đổi qua EditListing
            product.CheckAndUpdateStockStatus();

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
