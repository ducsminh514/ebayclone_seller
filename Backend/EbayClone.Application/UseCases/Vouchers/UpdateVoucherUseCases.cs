using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Shared.DTOs.Vouchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Vouchers
{
    public class UpdateVoucherUseCase
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IUnitOfWork _uow;

        public UpdateVoucherUseCase(IVoucherRepository voucherRepo, IUnitOfWork uow)
        {
            _voucherRepo = voucherRepo;
            _uow = uow;
        }

        public async Task ExecuteAsync(Guid id, Guid shopId, UpdateVoucherRequest req)
        {
            var voucher = await _voucherRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy voucher.");
            if (voucher.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa voucher này.");
            
            // Chỉ sửa khi DRAFT hoặc PAUSED
            if (voucher.Status != "DRAFT" && voucher.Status != "PAUSED")
                throw new InvalidOperationException("Chỉ có thể chỉnh sửa voucher ở trạng thái DRAFT hoặc PAUSED.");

            // Code không được sửa sau khi tạo
            // Cập nhật các fields cho phép
            if (!string.IsNullOrWhiteSpace(req.Name))
                voucher.Name = req.Name.Trim();
            if (req.MinOrderValue.HasValue)
                voucher.MinOrderValue = req.MinOrderValue.Value;
            if (req.MaxBudget.HasValue)
                voucher.MaxBudget = req.MaxBudget.Value;
            if (req.UsageLimit.HasValue)
                voucher.UsageLimit = req.UsageLimit.Value;
            if (req.PerBuyerLimit.HasValue)
                voucher.PerBuyerLimit = req.PerBuyerLimit.Value;
            if (!string.IsNullOrWhiteSpace(req.Visibility))
                voucher.Visibility = req.Visibility.ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(req.Scope))
                voucher.Scope = req.Scope.ToUpperInvariant();
            if (req.ProductIds != null)
                voucher.ProductIds = req.ProductIds;
            if (req.ValidFrom.HasValue)
                voucher.ValidFrom = req.ValidFrom.Value;
            if (req.ValidTo.HasValue)
                voucher.ValidTo = req.ValidTo.Value;
            if (req.MaxDiscountAmount.HasValue)
                voucher.MaxDiscountAmount = req.MaxDiscountAmount.Value;

            if (voucher.ValidTo <= voucher.ValidFrom)
                throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");

            _voucherRepo.Update(voucher);
            await _uow.SaveChangesAsync();
        }
    }

    public class UpdateVoucherStatusUseCase
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IUnitOfWork _uow;

        public UpdateVoucherStatusUseCase(IVoucherRepository voucherRepo, IUnitOfWork uow)
        {
            _voucherRepo = voucherRepo;
            _uow = uow;
        }

        /// <summary>
        /// Đổi status voucher. Transitions hợp lệ:
        /// DRAFT → ACTIVE (launch)
        /// ACTIVE → PAUSED
        /// PAUSED → ACTIVE (resume)
        /// ACTIVE/PAUSED → ENDED (manual end)
        /// </summary>
        public async Task ExecuteAsync(Guid id, Guid shopId, string newStatus)
        {
            var voucher = await _voucherRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy voucher.");
            if (voucher.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền thay đổi trạng thái voucher này.");

            newStatus = newStatus.ToUpperInvariant();

            // Validate transition
            var validTransitions = new Dictionary<string, List<string>>
            {
                ["DRAFT"]  = new() { "ACTIVE" },
                ["ACTIVE"] = new() { "PAUSED", "ENDED" },
                ["PAUSED"] = new() { "ACTIVE", "ENDED" },
                ["ENDED"]  = new() { }
            };

            if (!validTransitions.ContainsKey(voucher.Status) || !validTransitions[voucher.Status].Contains(newStatus))
                throw new InvalidOperationException($"Không thể chuyển từ '{voucher.Status}' sang '{newStatus}'.");

            // Validate khi launch (DRAFT → ACTIVE): check ValidTo còn trong tương lai
            if (newStatus == "ACTIVE" && voucher.ValidTo <= DateTimeOffset.UtcNow)
                throw new InvalidOperationException("Không thể khởi chạy voucher đã hết hạn. Hãy cập nhật ValidTo trước.");

            voucher.Status = newStatus;
            _voucherRepo.Update(voucher);
            await _uow.SaveChangesAsync();
        }
    }

    public class DeleteVoucherUseCase
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IUnitOfWork _uow;

        public DeleteVoucherUseCase(IVoucherRepository voucherRepo, IUnitOfWork uow)
        {
            _voucherRepo = voucherRepo;
            _uow = uow;
        }

        public async Task ExecuteAsync(Guid id, Guid shopId)
        {
            var voucher = await _voucherRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy voucher.");
            if (voucher.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa voucher này.");
            if (voucher.Status != "DRAFT")
                throw new InvalidOperationException("Chỉ có thể xóa voucher ở trạng thái DRAFT.");

            await _voucherRepo.DeleteAsync(id);
            await _uow.SaveChangesAsync();
        }
    }
}
