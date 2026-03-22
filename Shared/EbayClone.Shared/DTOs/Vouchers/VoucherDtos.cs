using System;

namespace EbayClone.Shared.DTOs.Vouchers
{
    // ── Request DTOs ─────────────────────────────────────────────────────

    public class CreateVoucherRequest
    {
        /// <summary>Mã giảm giá, max 15 ký tự, unique per shop.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Tên campaign nội bộ (buyer không thấy).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>PERCENTAGE | FIXED_AMOUNT</summary>
        public string DiscountType { get; set; } = "PERCENTAGE";

        public decimal Value { get; set; }

        /// <summary>Bắt buộc cho PERCENTAGE. Cap tối đa discount.</summary>
        public decimal? MaxDiscountAmount { get; set; }

        public decimal MinOrderValue { get; set; } = 0;

        /// <summary>Tổng ngân sách campaign. Null = không giới hạn.</summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>Số lần dùng tối đa (all buyers). 0 = unlimited.</summary>
        public int UsageLimit { get; set; } = 100;

        /// <summary>Mỗi buyer dùng tối đa bao nhiêu lần.</summary>
        public int PerBuyerLimit { get; set; } = 1;

        /// <summary>PUBLIC | PRIVATE</summary>
        public string Visibility { get; set; } = "PRIVATE";

        /// <summary>SHOP | PRODUCTS</summary>
        public string Scope { get; set; } = "SHOP";

        /// <summary>JSON array Guid strings khi Scope=PRODUCTS.</summary>
        public string? ProductIds { get; set; }

        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
    }

    public class UpdateVoucherRequest
    {
        public string? Name { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxBudget { get; set; }
        public int? UsageLimit { get; set; }
        public int? PerBuyerLimit { get; set; }
        public string? Visibility { get; set; }
        public string? Scope { get; set; }
        public string? ProductIds { get; set; }
        public DateTimeOffset? ValidFrom { get; set; }
        public DateTimeOffset? ValidTo { get; set; }
    }

    public class UpdateVoucherStatusRequest
    {
        /// <summary>ACTIVE | PAUSED | ENDED</summary>
        public string Status { get; set; } = string.Empty;
    }

    public class ApplyVoucherRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    // ── Response DTOs ─────────────────────────────────────────────────────

    public class VoucherDto
    {
        public Guid Id { get; set; }
        /// <summary>ShopId của voucher — cần cho Frontend khi gọi PreviewDiscount.</summary>
        public Guid ShopId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxBudget { get; set; }
        public decimal UsedBudget { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int PerBuyerLimit { get; set; }
        public string Visibility { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public string? ProductIds { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }

        // Computed helpers
        public bool IsExpired => DateTimeOffset.UtcNow > ValidTo;
        public int RemainingUses => UsageLimit == 0 ? int.MaxValue : UsageLimit - UsedCount;
    }

    public class ApplyVoucherResponse
    {
        public Guid VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal OriginalSubtotal { get; set; }
        public decimal FinalSubtotal => OriginalSubtotal - DiscountAmount;
        public string Message { get; set; } = string.Empty;
    }

    // ── Request DTO dùng cho POST /api/vouchers/apply (Preview) ────────────
    // Đặt ở Shared để cả Frontend và Backend dùng cùng 1 typed DTO (type-safe).
    public class ApplyVoucherPreviewRequest
    {
        public string Code { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public decimal ItemSubtotal { get; set; }
        public List<Guid>? ProductIds { get; set; }
    }
}
