using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Ghi nhận lỗi seller (defect) — ảnh hưởng Seller Level evaluation.
    /// Theo eBay: defect khi cancel OUT_OF_STOCK, dispute buyer win, late shipment.
    /// </summary>
    public class SellerDefect
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }

        /// <summary>
        /// Loại defect: CANCEL_OUT_OF_STOCK, CASE_CLOSED_BUYER_WIN, LATE_SHIPMENT
        /// </summary>
        public string DefectType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        public Shop? Shop { get; set; }
        public Order? Order { get; set; }
    }

    public static class DefectTypes
    {
        public const string CANCEL_OUT_OF_STOCK = "CANCEL_OUT_OF_STOCK";
        public const string CASE_CLOSED_BUYER_WIN = "CASE_CLOSED_BUYER_WIN";
        public const string LATE_SHIPMENT = "LATE_SHIPMENT";
    }
}
