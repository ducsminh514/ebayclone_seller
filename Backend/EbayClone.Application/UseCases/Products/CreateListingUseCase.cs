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
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateListingUseCase(
            IProductRepository productRepository,
            IShopRepository shopRepository,
            IPolicyRepository policyRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _shopRepository = shopRepository;
            _policyRepository = policyRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreateListingRequest request, CancellationToken cancellationToken = default)
        {
            // [C1] Validate Title length (3-80 ký tự — chuẩn eBay Cassini, không phải 255)
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3 || request.Name.Length > 80)
                throw new ArgumentException("Tiêu đề sản phẩm (Title) phải từ 3 đến 80 ký tự (chuẩn eBay).");

            // [C1] Validate Subtitle length (max 55 ký tự — eBay charge phí $1.50-$3.00 cho subtitle)
            if (!string.IsNullOrEmpty(request.Subtitle) && request.Subtitle.Length > 55)
                throw new ArgumentException("Phụ đề (Subtitle) không được vượt quá 55 ký tự (chuẩn eBay).");

            // [CRITICAL-1] Validate PrimaryImageUrl là bắt buộc (eBay không cho đăng không ảnh)
            if (string.IsNullOrWhiteSpace(request.PrimaryImageUrl))
                throw new ArgumentException("Ảnh bìa (Primary Image) là bắt buộc. Vui lòng upload ít nhất 1 ảnh sản phẩm.");

            // [WARNING-4] Validate ImageUrls max 12 ảnh phụ
            // eBay thực tế cho 24 ảnh miễn phí từ Nov 2022. Clone giới hạn 12 là trade-off hợp lý.
            if (request.ImageUrls != null && request.ImageUrls.Count > 12)
                throw new ArgumentException("Chỉ được upload tối đa 12 ảnh phụ.");

            // [C2] Validate Condition whitelist (chuẩn eBay 2024-2025)
            // "New Other" đã bị deprecated từ 2024 — KHÔNG còn hợp lệ
            // Pre-owned Excellent/Good/Fair được thêm từ Feb 2025 (clothing categories)
            // "New with defects" đổi tên thành "New with imperfections" từ Feb 2025
            var validConditions = new[]
            {
                // Nhóm New
                "New",
                "New with tags",
                "New without tags",
                "New with imperfections",   // đổi tên từ "New with defects" — Feb 2025
                "Open Box",
                // Nhóm Refurbished
                "Certified Refurbished",
                "Excellent Refurbished",
                "Very Good Refurbished",
                "Good Refurbished",
                "Seller Refurbished",
                // Nhóm Used / Pre-owned
                "Used",
                "Pre-owned - Excellent",    // mới Feb 2025 (clothing)
                "Pre-owned - Good",         // mới Feb 2025 (clothing)
                "Pre-owned - Fair",         // mới Feb 2025 (clothing)
                // Nhóm khác
                "For Parts or Not Working"
            };
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

            // [A4-FIX] Variation listing PHẢI dùng Fixed Price (eBay rule)
            if (request.IsVariationListing && request.ListingFormat == "AUCTION")
                throw new ArgumentException("Variation listings must use Fixed Price format. Auction is not supported for multi-variation items.");

            // [A4-FIX] RequireImmediatePayment chỉ áp dụng cho Fixed Price (eBay rule)
            if (request.RequireImmediatePayment && request.ListingFormat == "AUCTION")
                request.RequireImmediatePayment = false; // Silently reset — auction không hỗ trợ

            // [A3] Validate Best Offer logic
            if (request.AutoAcceptPrice.HasValue && request.AutoDeclinePrice.HasValue
                && request.AutoAcceptPrice <= request.AutoDeclinePrice)
                throw new ArgumentException("AutoAcceptPrice phải lớn hơn AutoDeclinePrice.");

            // [FIX-13] Validate ScheduledAt phải ở tương lai (defense-in-depth)
            if (request.ScheduledAt.HasValue && request.ScheduledAt.Value <= DateTimeOffset.UtcNow.AddMinutes(1))
                throw new ArgumentException("Thời gian hẹn giờ phải nằm trong tương lai (ít nhất 1 phút từ bây giờ).");

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

            // [FIX-1] Validate SkuCode unique per listing
            var skuCodes = request.Variants.Select(v => v.SkuCode).ToList();
            var duplicateSkus = skuCodes.GroupBy(s => s).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateSkus.Any())
                throw new ArgumentException($"SkuCode bị trùng trong cùng listing: {string.Join(", ", duplicateSkus)}. Mỗi biến thể phải có SKU riêng biệt.");

            // [FIX-2] Validate duplicate attribute keys per variant
            // [WARNING-3] Attribute chỉ bắt buộc khi có NHIỀU variants — để phân biệt chúng
            // Single-variant product không cần attribute (eBay cho phép listing không có variation attributes)
            bool multipleVariants = request.Variants.Count > 1;

            foreach (var vReq in request.Variants)
            {
                if (multipleVariants && (vReq.Attributes == null || !vReq.Attributes.Any()))
                    throw new ArgumentException($"Khi listing có nhiều biến thể, mỗi biến thể phải có ít nhất 1 thuộc tính (VD: Color - Red) để phân biệt. Biến thể '{vReq.SkuCode}' đang thiếu thuộc tính.");

                if (vReq.Attributes != null)
                {
                    var duplicateKeys = vReq.Attributes.Keys
                        .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicateKeys.Any())
                        throw new ArgumentException($"Biến thể '{vReq.SkuCode}' có thuộc tính trùng tên: {string.Join(", ", duplicateKeys)}.");
                }
            }

            // [FIX-5] Validate DUPLICATE attribute combination (eBay rule: "Duplicate name-value combinations not permitted")
            // VD: Variant A: {Color:Red, Size:M} + Variant B: {Color:Red, Size:M} → REJECTED
            var combinationSet = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var vReq in request.Variants)
            {
                if (vReq.Attributes == null) continue;
                // Tạo canonical key từ attribute combination (sort key để tránh order-sensitive false positive)
                var comboKey = string.Join("|", vReq.Attributes
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key.ToLower()}:{kv.Value.ToLower()}"));

                if (!combinationSet.Add(comboKey))
                    throw new ArgumentException(
                        $"Biến thể '{vReq.SkuCode}' có combination thuộc tính trùng với biến thể khác ({comboKey.Replace("|", ", ")}). " +
                        "eBay không cho phép 2 biến thể có cùng combination giá trị thuộc tính.");
            }

            // [FIX-6] Validate ATTRIBUTE KEY CONSISTENCY — tất cả variants phải dùng CÙNG BỘ attribute keys
            // eBay yêu cầu variation matrix nhất quán (không được vừa có Color+Size, vừa chỉ có Color)
            var firstVariantKeys = request.Variants[0].Attributes?.Keys
                .Select(k => k.ToLower())
                .OrderBy(k => k)
                .ToList() ?? new System.Collections.Generic.List<string>();

            for (int vi = 1; vi < request.Variants.Count; vi++)
            {
                var vReq = request.Variants[vi];
                var thisKeys = vReq.Attributes?.Keys
                    .Select(k => k.ToLower())
                    .OrderBy(k => k)
                    .ToList() ?? new System.Collections.Generic.List<string>();

                if (!thisKeys.SequenceEqual(firstVariantKeys))
                    throw new ArgumentException(
                        $"Biến thể '{vReq.SkuCode}' dùng bộ thuộc tính [{string.Join(", ", thisKeys)}] " +
                        $"khác với biến thể đầu tiên [{string.Join(", ", firstVariantKeys)}]. " +
                        "Tất cả biến thể trong cùng listing phải có cùng bộ thuộc tính (VD: đều dùng Color + Size).");
            }

            // [FIX-7] Validate Variation attribute names KHÔNG ĐƯỢC TRÙNG với Item Specifics names
            // eBay error: "Variations Specifics and Item Specifics entered for a Multi-SKU item should be different"
            if (request.ItemSpecifics != null && request.ItemSpecifics.Any() && request.Variants.Count > 0)
            {
                var itemSpecificNames = request.ItemSpecifics
                    .Select(s => s.Name.ToLower())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var variationAttrNames = firstVariantKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var conflictNames = variationAttrNames.Intersect(itemSpecificNames).ToList();
                if (conflictNames.Any())
                    throw new ArgumentException(
                        $"Thuộc tính variation [{string.Join(", ", conflictNames)}] trùng tên với Item Specifics. " +
                        "eBay yêu cầu Variation Attributes và Item Specifics phải có tên khác nhau hoàn toàn.");
            }


            // [CRITICAL-2] Validate CategoryId tồn tại trong DB
            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                throw new ArgumentException($"Danh mục với Id '{request.CategoryId}' không tồn tại.");

            // [WARNING-5] Validate Shop tồn tại — không skip MonthlyListingLimit nếu shop không tìm thấy
            var shop = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
            if (shop == null)
                throw new UnauthorizedAccessException("Shop không tồn tại hoặc token không hợp lệ.");

            // Kiểm tra giới hạn đăng bài hàng tháng (MonthlyListingLimit)
            // [Phase 3D] Below Standard Restriction: giảm limit 50%
            var effectiveLimit = shop.MonthlyListingLimit;
            var isRestricted = shop.SellerLevel == SellerLevels.BELOW_STANDARD;
            if (isRestricted)
            {
                effectiveLimit = (int)Math.Ceiling(shop.MonthlyListingLimit * 0.5);
            }

            var countThisMonth = await _productRepository.CountProductsThisMonthAsync(shopId, cancellationToken);
            if (countThisMonth >= effectiveLimit)
            {
                var reason = isRestricted
                    ? $"Seller Below Standard: giới hạn giảm còn {effectiveLimit} SP/tháng (gốc: {shop.MonthlyListingLimit}). Cải thiện defect rate để nâng limit."
                    : $"Bạn đã tạo {countThisMonth}/{effectiveLimit} sản phẩm trong tháng này. Hãy nâng cấp gói hoặc chờ tháng sau.";
                throw new InvalidOperationException(reason);
            }

            // Fallback to defaults if not provided
            // [FIX-H5] SECURITY: Validate policy belongs to THIS shop (prevent IDOR attack)
            var shippingPolicyId = request.ShippingPolicyId;
            if (shippingPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultShippingPolicyAsync(shopId, cancellationToken);
                shippingPolicyId = def?.Id;
            }
            else
            {
                // Validate ownership — seller A không được dùng policy của shop B
                var sp = await _policyRepository.GetShippingPolicyByIdAsync(shippingPolicyId.Value, cancellationToken);
                if (sp == null || sp.ShopId != shopId)
                    throw new UnauthorizedAccessException("Shipping policy không tồn tại hoặc không thuộc shop của bạn.");
            }

            var returnPolicyId = request.ReturnPolicyId;
            if (returnPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultReturnPolicyAsync(shopId, cancellationToken);
                returnPolicyId = def?.Id;
            }
            else
            {
                var rp = await _policyRepository.GetReturnPolicyByIdAsync(returnPolicyId.Value, cancellationToken);
                if (rp == null || rp.ShopId != shopId)
                    throw new UnauthorizedAccessException("Return policy không tồn tại hoặc không thuộc shop của bạn.");
            }

            var paymentPolicyId = request.PaymentPolicyId;
            if (paymentPolicyId == null)
            {
                var def = await _policyRepository.GetDefaultPaymentPolicyAsync(shopId, cancellationToken);
                paymentPolicyId = def?.Id;
            }
            else
            {
                var pp = await _policyRepository.GetPaymentPolicyByIdAsync(paymentPolicyId.Value, cancellationToken);
                if (pp == null || pp.ShopId != shopId)
                    throw new UnauthorizedAccessException("Payment policy không tồn tại hoặc không thuộc shop của bạn.");
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
                    BasePrice = request.Variants.Min(v => v.Price), // Min variant price làm giá base (eBay convention)
                    // [A4] Listing Meta
                    RequireImmediatePayment = request.RequireImmediatePayment,
                    IsVariationListing = request.IsVariationListing,
                    // [SHIPPING] Package Info
                    CountryOfOrigin = request.CountryOfOrigin?.ToUpperInvariant(),
                    PackageLengthCm = request.PackageLengthCm,
                    PackageWidthCm = request.PackageWidthCm,
                    PackageHeightCm = request.PackageHeightCm,
                };

                await _productRepository.AddAsync(product, cancellationToken);

                // [PERF Phase 2] Update denormalized count khi tạo listing mới
                if (product.Status == "DRAFT")
                {
                    shop.DraftListingCount++;
                    _shopRepository.Update(shop);
                }
                // SCHEDULED sẽ được ScheduledListingActivatorService update ActiveListingCount khi activate

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
                        ImageUrl = vReq.ImageUrl,
                        WeightGram = vReq.WeightGram
                    };

                    variantsToSave.Add(variant);
                }

                // 3. Batch Insert toàn bộ Variants
                await _productRepository.AddVariantsAsync(variantsToSave, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 3b. [A1] Tạo VariantAttributeValue relational entries
                // [FIX-3] Dùng request.Attributes trực tiếp thay vì parse lại từ JSON
                // → đảm bảo JSON và relational luôn từ cùng 1 nguồn data
                var attributeValues = new List<VariantAttributeValue>();
                for (int i = 0; i < variantsToSave.Count; i++)
                {
                    var savedVariant = variantsToSave[i];
                    var originalRequest = request.Variants[i];
                    if (originalRequest.Attributes == null || !originalRequest.Attributes.Any()) continue;
                    foreach (var kv in originalRequest.Attributes)
                    {
                        attributeValues.Add(new VariantAttributeValue
                        {
                            VariantId = savedVariant.Id,
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
