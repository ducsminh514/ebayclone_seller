using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Products;
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
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateProductStatusUseCase(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid productId, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);

            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sản phẩm này.");

            var newStatus = request.Status.ToUpper();

            // [C3] ENDED là trạng thái CUỐI CÙNG — seller có thể kết thúc listing bất kỳ lúc nào
            // Nhưng từ ENDED KHÔNG THỂ chuyển ngược lại (phải tạo listing mới = relist)
            if (product.Status == "ENDED")
                throw new InvalidOperationException("Listing đã kết thúc. Không thể thay đổi trạng thái. Hãy tạo listing mới (relist).");

            var allowedStatuses = new[] { "DRAFT", "ACTIVE", "SCHEDULED", "HIDDEN", "ENDED" };
            if (!allowedStatuses.Contains(newStatus))
                throw new ArgumentException("Trạng thái không hợp lệ.");

            // Validation đặc biệt cho SCHEDULED: bắt buộc phải kèm ScheduledAt trong tương lai
            if (newStatus == "SCHEDULED")
            {
                if (!request.ScheduledAt.HasValue)
                    throw new ArgumentException("Bạn phải chọn thời gian hẹn giờ khi đổi sang trạng thái SCHEDULED.");

                if (request.ScheduledAt.Value <= DateTimeOffset.UtcNow)
                    throw new ArgumentException("Thời gian hẹn giờ phải ở trong tương lai.");

                product.ScheduledAt = request.ScheduledAt;
            }
            else
            {
                // Khi đổi sang ACTIVE / HIDDEN / DRAFT → xóa ScheduledAt (hủy hẹn giờ)
                product.ScheduledAt = null;
            }

            // [A5] Publish Validation: DRAFT/SCHEDULED → ACTIVE
            // Khi publish (ACTIVE), validate tất cả REQUIRED item specifics đã điền
            if (newStatus == "ACTIVE" && (product.Status == "DRAFT" || product.Status == "SCHEDULED"))
            {
                var requiredSpecifics = (await _categoryRepository
                    .GetItemSpecificsByCategoryIdAsync(product.CategoryId, cancellationToken))
                    .Where(s => s.Requirement == "REQUIRED")
                    .ToList();

                if (requiredSpecifics.Any())
                {
                    var filledNames = product.ItemSpecifics?
                        .Select(s => s.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new System.Collections.Generic.HashSet<string>();

                    var missingSpecifics = requiredSpecifics
                        .Where(r => !filledNames.Contains(r.Name))
                        .Select(r => r.Name)
                        .ToList();

                    if (missingSpecifics.Any())
                    {
                        throw new ArgumentException(
                            $"Không thể đăng bán. Thiếu Item Specifics bắt buộc: {string.Join(", ", missingSpecifics)}. " +
                            "Vui lòng điền đầy đủ trước khi Active sản phẩm.");
                    }
                }
            }

            product.Status = newStatus;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
