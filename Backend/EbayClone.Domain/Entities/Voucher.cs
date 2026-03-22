using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Coded Coupon — chuẩn eBay 2024.
    /// Status lifecycle: DRAFT → ACTIVE → PAUSED → ENDED
    /// DiscountType: PERCENTAGE | FIXED_AMOUNT
    /// Scope: SHOP | PRODUCTS
    /// Visibility: PUBLIC | PRIVATE
    /// </summary>
    public class Voucher
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }

        // ── Basic Info ──────────────────────────────────────────────────
        /// <summary>Mã giảm giá buyer nhập. Max 15 ký tự, unique per shop.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên campaign nội bộ — buyer KHÔNG thấy.</summary>
        public string Name { get; set; } = string.Empty;

        // ── Discount ────────────────────────────────────────────────────
        /// <summary>PERCENTAGE | FIXED_AMOUNT</summary>
        public string DiscountType { get; set; } = "PERCENTAGE";

        /// <summary>Giá trị giảm: số % hoặc số tiền cố định.</summary>
        public decimal Value { get; set; }

        /// <summary>
        /// [PERCENTAGE only] Cap tối đa discount. Bắt buộc khi DiscountType=PERCENTAGE.
        /// VD: 20% off nhưng max 100k → discount = min(20% × subtotal, 100k).
        /// </summary>
        public decimal? MaxDiscountAmount { get; set; }

        // ── Conditions ──────────────────────────────────────────────────
        /// <summary>Đơn tối thiểu để áp dụng.</summary>
        public decimal MinOrderValue { get; set; } = 0;

        // ── Budget Control ──────────────────────────────────────────────
        /// <summary>
        /// Tổng ngân sách campaign. Khi UsedBudget >= MaxBudget → auto-disable.
        /// Null = không giới hạn ngân sách.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>Tổng số tiền đã discount (dùng cho budget tracking).</summary>
        public decimal UsedBudget { get; set; } = 0;

        // ── Usage Limits ────────────────────────────────────────────────
        /// <summary>Số lần dùng tối đa (tất cả buyers). 0 = unlimited.</summary>
        public int UsageLimit { get; set; } = 100;

        /// <summary>Số lần đã dùng.</summary>
        public int UsedCount { get; set; } = 0;

        /// <summary>Mỗi buyer dùng tối đa bao nhiêu lần. Default = 1.</summary>
        public int PerBuyerLimit { get; set; } = 1;

        // ── Scope & Visibility ──────────────────────────────────────────
        /// <summary>PUBLIC: hiện trên listing. PRIVATE: seller tự share.</summary>
        public string Visibility { get; set; } = "PRIVATE";

        /// <summary>SHOP: toàn shop. PRODUCTS: chỉ sản phẩm trong ProductIds.</summary>
        public string Scope { get; set; } = "SHOP";

        /// <summary>
        /// JSON array of Product Guid strings khi Scope=PRODUCTS.
        /// VD: "[\"guid1\",\"guid2\"]"
        /// </summary>
        public string? ProductIds { get; set; }

        // ── Lifecycle ───────────────────────────────────────────────────
        /// <summary>DRAFT | ACTIVE | PAUSED | ENDED</summary>
        public string Status { get; set; } = "DRAFT";

        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }

        // Optimistic Concurrency Token (chống race condition)
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public Shop? Shop { get; set; }

        // ── Domain Methods ──────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra voucher có thể dùng được không (không check PerBuyerLimit — cần query DB).
        /// </summary>
        public bool IsUsable(decimal orderSubtotal, DateTimeOffset now)
        {
            if (Status != "ACTIVE") return false;
            if (now < ValidFrom || now > ValidTo) return false;
            if (UsageLimit > 0 && UsedCount >= UsageLimit) return false;
            if (MaxBudget.HasValue && UsedBudget >= MaxBudget.Value) return false;
            if (orderSubtotal < MinOrderValue) return false;
            return true;
        }

        /// <summary>
        /// Tính discount amount cho order.
        /// PERCENTAGE: min(Value% × subtotal, MaxDiscountAmount).
        /// FIXED_AMOUNT: min(Value, subtotal) — không giảm âm.
        /// </summary>
        public decimal CalculateDiscount(decimal orderSubtotal)
        {
            decimal discount;
            if (DiscountType == "PERCENTAGE")
            {
                discount = orderSubtotal * Value / 100m;
                if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
                    discount = MaxDiscountAmount.Value;
            }
            else // FIXED_AMOUNT
            {
                discount = Value;
            }

            // Không giảm quá subtotal
            return Math.Min(discount, orderSubtotal);
        }

        /// <summary>Parse ProductIds JSON thành list Guid.</summary>
        public List<Guid> GetProductIdList()
        {
            if (string.IsNullOrWhiteSpace(ProductIds)) return new List<Guid>();
            try
            {
                var ids = JsonSerializer.Deserialize<List<string>>(ProductIds) ?? new List<string>();
                var result = new List<Guid>();
                foreach (var s in ids)
                    if (Guid.TryParse(s, out var g)) result.Add(g);
                return result;
            }
            catch { return new List<Guid>(); }
        }
    }
}

