using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
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
        private readonly IUnitOfWork _unitOfWork;

        public CreateListingUseCase(
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid shopId, CreateListingRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Variants == null || request.Variants.Count == 0)
                throw new ArgumentException("At least one variant is required");

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1. Khởi tạo đối tượng Product (Entity Cha)
                var product = new Product
                {
                    ShopId = shopId,
                    CategoryId = request.CategoryId,
                    ShippingPolicyId = request.ShippingPolicyId,
                    ReturnPolicyId = request.ReturnPolicyId,
                    Name = request.Name,
                    Description = request.Description,
                    Brand = request.Brand,
                    Status = "DRAFT", // Đưa vào trạng thái nháp, chờ Publish
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
