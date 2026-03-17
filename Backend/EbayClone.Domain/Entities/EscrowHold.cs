using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// [Phase 3] Track escrow hold per order.
    /// Khi buyer trả tiền → tạo EscrowHold (HOLDING).
    /// Sau hold period (dựa trên seller level) → release sang Available.
    /// Khi return/dispute → giữ nguyên hold cho đến khi resolve.
    /// </summary>
    public class EscrowHold
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid OrderId { get; set; }
        
        public decimal Amount { get; set; }
        
        /// <summary>
        /// HOLDING: Đang giữ, chờ release.
        /// RELEASED: Đã chuyển sang AvailableBalance.
        /// REFUNDED: Đã trả lại buyer (return/dispute).
        /// DISPUTE_HOLD: Bị lock do return/dispute mở.
        /// </summary>
        public string Status { get; set; } = "HOLDING";
        
        /// <summary>Thời điểm bắt đầu hold (= thời điểm buyer pay).</summary>
        public DateTimeOffset HoldStartedAt { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>Thời điểm dự kiến release (= HoldStartedAt + Shop.GetHoldDays()).</summary>
        public DateTimeOffset HoldReleasesAt { get; set; }
        
        /// <summary>Thời điểm thực tế release/refund (null nếu đang hold).</summary>
        public DateTimeOffset? ResolvedAt { get; set; }
        
        /// <summary>Ghi chú khi resolve (VD: "Released after hold period", "Refunded via dispute").</summary>
        public string? ResolveNote { get; set; }
        
        // Navigation
        public Shop? Shop { get; set; }
        public Order? Order { get; set; }
    }
}
