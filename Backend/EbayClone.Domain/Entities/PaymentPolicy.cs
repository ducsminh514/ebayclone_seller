using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Payment Policy - Thiết lập điều khoản thanh toán cho listing.
    /// Trên eBay thật (Managed Payments 2024): Buyer có thể thanh toán bằng Credit Card,
    /// Apple Pay, Google Pay, PayPal. Seller chỉ cần cài đặt:
    /// - Có yêu cầu thanh toán ngay (Immediate Payment) cho Buy It Now hay không
    /// - Thời hạn thanh toán cho Auction (2-10 ngày)
    /// - Phương thức thanh toán chấp nhận
    /// 
    /// PERFORMANCE NOTE: ShopId có Index để tăng tốc truy vấn theo cửa hàng.
    /// SECURITY NOTE: Không lưu thông tin thanh toán nhạy cảm trong entity này.
    /// </summary>
    public class PaymentPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Core eBay Payment Settings
        /// <summary>
        /// Yêu cầu thanh toán ngay cho Buy It Now.
        /// Trên eBay thật: Đây là best practice để chống ngâm đơn (unpaid items).
        /// </summary>
        public bool ImmediatePaymentRequired { get; set; } = true;

        /// <summary>
        /// Số ngày cho phép thanh toán sau (cho Auction).
        /// eBay thật cho phép 2, 3, 5, 7, 10 ngày.
        /// </summary>
        public int DaysToPayment { get; set; } = 4;

        /// <summary>
        /// Phương thức thanh toán chính.
        /// eBay Managed Payments: "ManagedPayments" (default).
        /// Legacy: "PayPal", "CreditCard", "BankTransfer"
        /// </summary>
        public string PaymentMethod { get; set; } = "ManagedPayments";

        /// <summary>
        /// Hướng dẫn thanh toán cho buyer (tùy chọn, max 500 ký tự).
        /// </summary>
        public string? PaymentInstructions { get; set; }

        public bool IsDefault { get; set; } = false;

        // Management
        public bool IsArchived { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Shop? Shop { get; set; }
    }
}
