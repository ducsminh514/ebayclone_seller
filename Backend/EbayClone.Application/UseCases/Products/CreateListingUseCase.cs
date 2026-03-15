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
            if (request.Variants == null || request.Variants.Count == 0)
                throw new ArgumentException("At least one variant is required");

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
                    PaymentPolicyId = paymentPolicyId, // Added missing PaymentPolicyId mapping
                    Name = request.Name,
                    Description = request.Description,
                    Brand = request.Brand,
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

                // 4. Commit toàn bộ thay đổi thành 1 khối vững chắc
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
