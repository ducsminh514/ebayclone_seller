using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Vouchers;
using System;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Vouchers
{
    /// <summary>
    /// Tạo mới voucher. Validation đầy đủ theo nghiệp vụ.md Phần 5.
    /// </summary>
    public class CreateVoucherUseCase
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IShopRepository _shopRepo;
        private readonly IUnitOfWork _uow;

        public CreateVoucherUseCase(IVoucherRepository voucherRepo, IShopRepository shopRepo, IUnitOfWork uow)
        {
            _voucherRepo = voucherRepo;
            _shopRepo = shopRepo;
            _uow = uow;
        }

        public async Task<Voucher> ExecuteAsync(Guid shopId, CreateVoucherRequest req)
        {
            // 1. Shop exists & verified
            var shop = await _shopRepo.GetByIdAsync(shopId)
                ?? throw new InvalidOperationException("Shop không tồn tại.");
            if (!shop.IsVerified)
                throw new InvalidOperationException("Shop chưa được xác minh. Cần verify trước khi tạo voucher.");

            // 2. Validate Code format
            var code = req.Code?.Trim().ToUpperInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(code) || code.Length > 15)
                throw new ArgumentException("Code tối đa 15 ký tự và không được để trống.");

            // 3. Code unique per shop
            if (await _voucherRepo.CodeExistsForShopAsync(code, shopId))
                throw new InvalidOperationException($"Mã '{code}' đã tồn tại trong shop này.");

            // 4. Validate discount
            if (req.Value <= 0)
                throw new ArgumentException("Giá trị giảm phải lớn hơn 0.");

            if (req.DiscountType == "PERCENTAGE")
            {
                if (req.Value > 100)
                    throw new ArgumentException("Phần trăm giảm không được vượt quá 100%.");
                if (!req.MaxDiscountAmount.HasValue || req.MaxDiscountAmount.Value <= 0)
                    throw new ArgumentException("Voucher PERCENTAGE bắt buộc phải có MaxDiscountAmount > 0 để tránh seller bị lỗ.");
            }

            // 5. Validate conditions
            if (req.MinOrderValue < 0)
                throw new ArgumentException("Đơn tối thiểu không được âm.");
            if (req.UsageLimit < 0)
                throw new ArgumentException("Số lần dùng tối đa không được âm (0 = unlimited).");
            if (req.PerBuyerLimit < 1)
                throw new ArgumentException("Mỗi buyer phải được dùng ít nhất 1 lần.");
            if (req.MaxBudget.HasValue && req.MaxBudget.Value <= 0)
                throw new ArgumentException("Ngân sách tối đa phải lớn hơn 0 nếu được thiết lập.");

            // 6. Validate dates
            if (req.ValidTo <= req.ValidFrom)
                throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");

            // 7. Validate scope
            var scope = (req.Scope ?? "SHOP").ToUpperInvariant();
            if (scope != "SHOP" && scope != "PRODUCTS")
                throw new ArgumentException("Scope phải là SHOP hoặc PRODUCTS.");
            if (scope == "PRODUCTS" && string.IsNullOrWhiteSpace(req.ProductIds))
                throw new ArgumentException("Scope PRODUCTS yêu cầu danh sách ProductIds.");

            var voucher = new Voucher
            {
                ShopId = shopId,
                Code = code,
                Name = req.Name?.Trim() ?? code,
                DiscountType = (req.DiscountType ?? "PERCENTAGE").ToUpperInvariant(),
                Value = req.Value,
                MaxDiscountAmount = req.MaxDiscountAmount,
                MinOrderValue = req.MinOrderValue,
                MaxBudget = req.MaxBudget,
                UsageLimit = req.UsageLimit,
                PerBuyerLimit = req.PerBuyerLimit,
                Visibility = (req.Visibility ?? "PRIVATE").ToUpperInvariant(),
                Scope = scope,
                ProductIds = req.ProductIds,
                Status = "DRAFT",
                ValidFrom = req.ValidFrom,
                ValidTo = req.ValidTo,
            };

            await _voucherRepo.AddAsync(voucher);
            await _uow.SaveChangesAsync();
            return voucher;
        }
    }
}
