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
    public interface ICreateListingUseCase
    {
        Task<Guid> ExecuteAsync(Guid shopId, CreateListingRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateListingUseCase : ICreateListingUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateListingUseCase(
            IProductRepository productRepository,
            IShopRepository shopRepository,
            IPolicyRepository policyRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreateListingRequest request, CancellationToken cancellationToken = default)
        {
            // [C1] Validate Title length (3-255 ký tự — chuẩn eBay)
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 255)
                throw new ArgumentException("Tiêu đề sản phẩm (Title) phải từ 3 đến 255 ký tự.");

            // [C1] Validate Subtitle length (max 80 ký tự — eBay charge phí phụ cho subtitle)
            if (!string.IsNullOrEmpty(request.Subtitle) && request.Subtitle.Length > 80)
                throw new ArgumentException("Phụ đề (Subtitle) không được vượt quá 80 ký tự.");

            // [C2] Validate Condition whitelist (chuẩn eBay 2024)
            var validConditions = new[] { "New", "New Other", "Open Box", "Seller Refurbished", "Used", "For Parts" };
            if (!validConditions.Contains(request.Condition))
                throw new ArgumentException($"Condition không hợp lệ. Giá trị cho phép: {string.Join(", ", validConditions)}");

            if (request.Variants == null || request.Variants.Count == 0)
                throw new ArgumentException("At least one variant is required");

            // [A3] Validate ListingFormat
            var validFormats = new[] { "FIXED_PRICE", "AUCTION" };
            if (!validFormats.Contains(request.ListingFormat))
                throw new ArgumentException($"ListingFormat phải là FIXED_PRICE hoặc AUCTION.");

            // [A3] Auction + Variations = forbidden (quy tắc eBay)
            if (request.ListingFormat == "AUCTION" && request.Variants.Count > 1)
                throw new ArgumentException("Listing dạng AUCTION không hỗ trợ nhiều variations. Chỉ được 1 variant duy nhất.");

            // [A3] Validate Best Offer logic
            if (request.AutoAcceptPrice.HasValue && request.AutoDeclinePrice.HasValue
                && request.AutoAcceptPrice <= request.AutoDeclinePrice)
                throw new ArgumentException("AutoAcceptPrice phải lớn hơn AutoDeclinePrice.");

            // [A4] Validate Variation Limits
            if (request.Variants.Count > 250)
                throw new ArgumentException("Một listing không được vượt quá 250 biến thể (variations).");

            // [A4] Validate max 50 distinct options per attribute
            var allAttributes = request.Variants
                .Where(v => v.Attributes != null)
                .SelectMany(v => v.Attributes)
                .GroupBy(kv => kv.Key)
                .ToList();

            foreach (var attrGroup in allAttributes)
            {
                var distinctOptions = attrGroup.Select(kv => kv.Value).Distinct().Count();
                if (distinctOptions > 50)
                    throw new ArgumentException(
                        $"Thuộc tính '{attrGroup.Key}' có {distinctOptions} options, vượt quá giới hạn 50 options/attribute.");
            }

            // Kiểm tra giới hạn đăng bài hàng tháng (MonthlyListingLimit)
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop != null)
            {
                var countThisMonth = await _productRepository.CountProductsThisMonthAsync(shopId, cancellationToken);
                if (countThisMonth >= shop.MonthlyListingLimit)
                    throw new InvalidOperationException(
                        $"Bạn đã tạo {countThisMonth}/{shop.MonthlyListingLimit} sản phẩm trong tháng này. Hãy nâng cấp gói hoặc chờ tháng sau.");
            }

            // Fallback to defaults if not provided
            var shippingPolicyId = request.ShippingPolicyId;
            if (shippingPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultShippingPolicyAsync(shopId, cancellationToken);
                shippingPolicyId = def?.Id;
            }

            var returnPolicyId = request.ReturnPolicyId;
            if (returnPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultReturnPolicyAsync(shopId, cancellationToken);
                returnPolicyId = def?.Id;
            }

            var paymentPolicyId = request.PaymentPolicyId;
            if (paymentPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultPaymentPolicyAsync(shopId, cancellationToken);
                paymentPolicyId = def?.Id;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Khởi tạo đối tượng Product (Entity Cha)
                // Xử lý ảnh: PrimaryImageUrl lấy ảnh đầu tiên nếu ko chỉ định
                string? primaryImg = request.PrimaryImageUrl;
                if (string.IsNullOrEmpty(primaryImg) && request.ImageUrls != null && request.ImageUrls.Any())
                    primaryImg = request.ImageUrls[0];
                
                string? imageUrlsJson = request.ImageUrls != null && request.ImageUrls.Any()
                    ? JsonSerializer.Serialize(request.ImageUrls)
                    : null;
 
                var product = new Product
                {
                    ShopId = shopId,
                    CategoryId = request.CategoryId,
                    ShippingPolicyId = shippingPolicyId,
                    ReturnPolicyId = returnPolicyId,
                    PaymentPolicyId = paymentPolicyId,
                    Name = request.Name,
                    Description = request.Description,
                    Brand = request.Brand,
                    Condition = request.Condition,                   // [A2]
                    ConditionDescription = request.ConditionDescription, // [A2]
                    ListingFormat = request.ListingFormat,            // [A3]
                    AllowOffers = request.AllowOffers,                // [A3]
                    AutoAcceptPrice = request.AutoAcceptPrice,        // [A3]
                    AutoDeclinePrice = request.AutoDeclinePrice,      // [A3]
                    Subtitle = request.Subtitle,                     // [A3]
                    PrimaryImageUrl = primaryImg,
                    ImageUrls = imageUrlsJson,
                    ScheduledAt = request.ScheduledAt,
                    // Nếu seller chọn hẹn giờ → SCHEDULED, không thì DRAFT
                    Status = request.ScheduledAt.HasValue ? "SCHEDULED" : "DRAFT",
                    BasePrice = request.Variants[0].Price // Lấy giá biến thể đầu tiên làm giá base
                };

                await _productRepository.AddAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Lấy ProductId

                // 2. Khởi tạo danh sách Mảng Biến thể (SKUs - Entities Con)
                var variantsToSave = new List<ProductVariant>();

                foreach (var vReq in request.Variants)
                {
                    if (vReq.Attributes != null && vReq.Attributes.Count > 5)
                    {
                        throw new ArgumentException($"Biến thể {vReq.SkuCode} không được phép vượt quá 5 thuộc tính phân loại (Color, Size...).");
                    }
                    
                    // Chuyển đổi dữ liệu Dictionary Thuộc tính con thành chuỗi JSON
                    string attributesJson = JsonSerializer.Serialize(vReq.Attributes);

                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        SkuCode = vReq.SkuCode,
                        Price = vReq.Price,
                        Attributes = attributesJson,
                        Quantity = vReq.Quantity, // Total physical stock
                        ReservedQuantity = 0,
                        ImageUrl = vReq.ImageUrl,
                        WeightGram = vReq.WeightGram
                    };

                    variantsToSave.Add(variant);
                }

                // 3. Batch Insert toàn bộ Variants
                await _productRepository.AddVariantsAsync(variantsToSave, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 3b. [A1] Tạo VariantAttributeValue relational entries
                // Lưu song song JSON (quick read) + relational (query/filter)
                var attributeValues = new List<VariantAttributeValue>();
                foreach (var variant in variantsToSave)
                {
                    if (variant.Attributes == null) continue;
                    var attrs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(variant.Attributes);
                    if (attrs == null) continue;
                    foreach (var kv in attrs)
                    {
                        attributeValues.Add(new VariantAttributeValue
                        {
                            VariantId = variant.Id,
                            AttributeName = kv.Key,
                            AttributeValue = kv.Value
                        });
                    }
                }
                if (attributeValues.Count > 0)
                {
                    await _productRepository.AddVariantAttributeValuesAsync(attributeValues, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // 4. [A5] Save Item Specifics
                if (request.ItemSpecifics != null && request.ItemSpecifics.Count > 0)
                {
                    var specifics = request.ItemSpecifics.Select(s => new ProductItemSpecific
                    {
                        ProductId = product.Id,
                        Name = s.Name,
                        Value = s.Value
                    }).ToList();

                    await _productRepository.AddProductItemSpecificsAsync(specifics, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // 5. Commit toàn bộ thay đổi thành 1 khối vững chắc
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return product.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
