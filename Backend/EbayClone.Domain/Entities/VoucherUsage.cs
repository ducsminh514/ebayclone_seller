using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Tracking mỗi lần buyer dùng voucher — dùng để enforce PerBuyerLimit + audit trail.
    /// </summary>
    public class VoucherUsage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VoucherId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid OrderId { get; set; }

        /// <summary>Số tiền đã discount trong lần dùng này.</summary>
        public decimal DiscountAmount { get; set; }

        public DateTimeOffset UsedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        public Voucher? Voucher { get; set; }
    }
}
