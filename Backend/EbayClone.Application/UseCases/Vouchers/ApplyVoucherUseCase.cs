using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Vouchers;
using System;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Vouchers
{
    /// <summary>
    /// Apply voucher khi buyer checkout.
    /// 
    /// Có 2 entry points:
    ///   - PreviewAsync: validate + tính discount, KHÔNG gọi AtomicApply (dùng cho POST /api/vouchers/apply preview)
    ///   - ExecuteAsync: validate + tính discount + AtomicApply thật (dùng cho CreateTestOrderUseCase)
    /// </summary>
    public class ApplyVoucherUseCase
    {
        private readonly IVoucherRepository _voucherRepo;
        private readonly IUnitOfWork _uow;

        public ApplyVoucherUseCase(IVoucherRepository voucherRepo, IUnitOfWork uow)
        {
            _voucherRepo = voucherRepo;
            _uow = uow;
        }

        // ── Validation helper chung (Step 1-8) ───────────────────────────

        private async Task<(Voucher voucher, decimal discountAmount)> ValidateAndCalculateAsync(
            string code,
            Guid shopId,
            Guid buyerId,
            decimal itemSubtotal,
            System.Collections.Generic.List<Guid> productIdsInCart)
        {
            // Bước 1: Tìm voucher
            var voucher = await _voucherRepo.GetByCodeAndShopIdAsync(code.Trim().ToUpperInvariant(), shopId)
                ?? throw new InvalidOperationException("Mã giảm giá không tồn tại hoặc không áp dụng cho shop này.");

            var now = DateTimeOffset.UtcNow;

            // Bước 2-5: IsUsable() check (Status, dates, UsageLimit, MaxBudget, MinOrderValue)
            if (!voucher.IsUsable(itemSubtotal, now))
            {
                if (voucher.Status != "ACTIVE")
                    throw new InvalidOperationException("Mã giảm giá không hoạt động.");
                if (now < voucher.ValidFrom)
                    throw new InvalidOperationException($"Mã giảm giá chưa có hiệu lực. Hiệu lực từ: {voucher.ValidFrom:dd/MM/yyyy}.");
                if (now > voucher.ValidTo)
                    throw new InvalidOperationException("Mã giảm giá đã hết hạn.");
                if (itemSubtotal < voucher.MinOrderValue)
                    throw new InvalidOperationException($"Đơn hàng tối thiểu {voucher.MinOrderValue:N0}đ để sử dụng mã này (hiện tại: {itemSubtotal:N0}đ).");
                throw new InvalidOperationException("Mã giảm giá đã hết lượt sử dụng.");
            }

            // Bước 6: PerBuyerLimit check
            if (voucher.PerBuyerLimit > 0)
            {
                var buyerUsageCount = await _voucherRepo.GetUsageCountByBuyerAsync(voucher.Id, buyerId);
                if (buyerUsageCount >= voucher.PerBuyerLimit)
                    throw new InvalidOperationException($"Bạn đã sử dụng mã này {buyerUsageCount} lần. Giới hạn: {voucher.PerBuyerLimit} lần/người.");
            }

            // Bước 7: Scope check (PRODUCTS)
            if (voucher.Scope == "PRODUCTS")
            {
                var allowedIds = voucher.GetProductIdList();
                bool hasEligible = false;
                foreach (var pid in productIdsInCart)
                {
                    if (allowedIds.Contains(pid)) { hasEligible = true; break; }
                }
                if (!hasEligible)
                    throw new InvalidOperationException("Mã giảm giá không áp dụng cho sản phẩm trong đơn hàng này.");
            }

            // Bước 8: Tính discount
            var discountAmount = voucher.CalculateDiscount(itemSubtotal);

            return (voucher, discountAmount);
        }

        // ── Preview (không tốn lượt) ─────────────────────────────────────
        /// <summary>
        /// Validate voucher và trả về discount amount để preview.
        /// KHÔNG gọi AtomicApplyAsync — lượt voucher KHÔNG bị tiêu.
        /// Dùng cho endpoint POST /api/vouchers/apply.
        /// </summary>
        public async Task<ApplyVoucherResponse> PreviewAsync(
            string code,
            Guid shopId,
            Guid buyerId,
            decimal itemSubtotal,
            System.Collections.Generic.List<Guid> productIdsInCart)
        {
            var (voucher, discountAmount) = await ValidateAndCalculateAsync(
                code, shopId, buyerId, itemSubtotal, productIdsInCart);

            return new ApplyVoucherResponse
            {
                VoucherId = voucher.Id,
                Code = voucher.Code,
                DiscountAmount = discountAmount,
                OriginalSubtotal = itemSubtotal,
                Message = $"Mã hợp lệ! Giảm {discountAmount:N0}đ."
            };
        }

        // ── Execute (tiêu lượt thật — dùng khi tạo Order) ────────────────
        /// <summary>
        /// Validate + AtomicApply thật sự (giảm UsedCount, UsedBudget).
        /// Gọi trong CreateTestOrderUseCase khi buyer xác nhận mua.
        /// VoucherUsage sẽ được ghi bởi CreateTestOrderUseCase sau khi order save.
        /// </summary>
        public async Task<ApplyVoucherResponse> ExecuteAsync(
            string code,
            Guid shopId,
            Guid buyerId,
            decimal itemSubtotal,
            System.Collections.Generic.List<Guid> productIdsInCart)
        {
            var (voucher, discountAmount) = await ValidateAndCalculateAsync(
                code, shopId, buyerId, itemSubtotal, productIdsInCart);

            // Bước 9: AtomicApply (hành động cuối — commit nguyên tử)
            // KHÔNG gọi SaveChanges ở đây vì AtomicApply là raw SQL riêng.
            // VoucherUsage sẽ được ghi khi tạo Order (CreateTestOrderUseCase).
            var success = await _voucherRepo.AtomicApplyAsync(voucher.Id, discountAmount);
            if (!success)
                throw new InvalidOperationException("Mã giảm giá vừa hết lượt trong lúc bạn đặt hàng. Vui lòng thử lại hoặc dùng mã khác.");

            return new ApplyVoucherResponse
            {
                VoucherId = voucher.Id,
                Code = voucher.Code,
                DiscountAmount = discountAmount,
                OriginalSubtotal = itemSubtotal,
                Message = $"Áp dụng thành công! Giảm {discountAmount:N0}đ."
            };
        }
    }
}
