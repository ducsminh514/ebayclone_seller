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
                throw new ArgumentException("Sản phẩm không tồn tại.");

            if (product.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa sản phẩm này.");

            var newStatus = request.Status.ToUpper();
            var allowedStatuses = new[] { "DRAFT", "ACTIVE", "SCHEDULED", "HIDDEN" };
            if (!System.Linq.Enumerable.Contains(allowedStatuses, newStatus))
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

            product.Status = newStatus;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            await _productRepository.UpdateAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
