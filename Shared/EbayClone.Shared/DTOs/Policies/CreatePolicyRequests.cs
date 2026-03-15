using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Policies
{
    public class CreateShippingPolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 30, ErrorMessage = "Thời gian chuẩn bị hàng tối đa 30 ngày")]
        public int HandlingTimeDays { get; set; } = 2;

        public string Description { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        // Domestic
        [Required]
        public string DomesticCostType { get; set; } = "Flat";
        public List<ShippingServiceDto> DomesticServices { get; set; } = new();

        // International
        public bool IsInternationalShippingAllowed { get; set; } = false;
        public string InternationalCostType { get; set; } = "Flat";
        public List<InternationalShippingServiceDto> InternationalServices { get; set; } = new();

        // Preferences
        public List<string> ExcludedLocations { get; set; } = new();
    }

    public class CreateReturnPolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public bool IsDomesticAccepted { get; set; } = false;
        public int DomesticReturnDays { get; set; } = 30;

        [RegularExpression("BUYER|SELLER", ErrorMessage = "Chỉ chấp nhận BUYER hoặc SELLER")]
        public string DomesticShippingPaidBy { get; set; } = "BUYER";

        public bool IsInternationalAccepted { get; set; } = false;
        public int InternationalReturnDays { get; set; } = 30;

        [RegularExpression("BUYER|SELLER", ErrorMessage = "Chỉ chấp nhận BUYER hoặc SELLER")]
        public string InternationalShippingPaidBy { get; set; } = "BUYER";
    }

    public class CreatePaymentPolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Description { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Require immediate payment for Buy It Now listings
        /// </summary>
        public bool ImmediatePaymentRequired { get; set; } = true;

        /// <summary>
        /// Days allowed for payment (for Auction). eBay allows: 2, 3, 5, 7, 10
        /// </summary>
        [Range(1, 10, ErrorMessage = "Days to payment must be between 1 and 10")]
        public int DaysToPayment { get; set; } = 4;

        /// <summary>
        /// Payment method: ManagedPayments (default), PayPal, CreditCard
        /// </summary>
        [RegularExpression("ManagedPayments|PayPal|CreditCard|BankTransfer", ErrorMessage = "Invalid payment method")]
        public string PaymentMethod { get; set; } = "ManagedPayments";

        [MaxLength(500)]
        public string? PaymentInstructions { get; set; }
    }
}
