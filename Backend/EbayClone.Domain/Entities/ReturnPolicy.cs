using System;

namespace EbayClone.Domain.Entities
{
    public class ReturnPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Domestic
        public bool IsDomesticAccepted { get; set; } = false;
        public int DomesticReturnDays { get; set; } = 30;
        public string DomesticShippingPaidBy { get; set; } = "BUYER"; // 'BUYER' or 'SELLER'
        public string DomesticRefundMethod { get; set; } = "MoneyBack"; // MoneyBack, MoneyBackOrReplacement, MoneyBackOrExchange

        // International
        public bool IsInternationalAccepted { get; set; } = false;
        public int InternationalReturnDays { get; set; } = 30;
        public string InternationalShippingPaidBy { get; set; } = "BUYER";
        public string InternationalRefundMethod { get; set; } = "MoneyBack"; // MoneyBack, MoneyBackOrReplacement, MoneyBackOrExchange

        // Auto-Accept Returns (eBay: tự động chấp nhận return request từ buyer)
        public bool AutoAcceptReturns { get; set; } = false;

        // Send Immediate Refund (eBay: hoàn tiền ngay khi buyer gửi hàng trả)
        public bool SendImmediateRefund { get; set; } = false;

        // Return Address (eBay: cho phép dùng địa chỉ khác với địa chỉ shop chính)
        public string? ReturnAddressJson { get; set; } // { name, street, city, state, zip, country }



        public bool IsDefault { get; set; } = false;
 
        // Management
        public bool IsArchived { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Shop? Shop { get; set; }
    }
}
