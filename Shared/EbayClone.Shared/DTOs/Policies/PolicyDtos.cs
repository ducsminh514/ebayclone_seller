using System;
using System.Collections.Generic;

namespace EbayClone.Shared.DTOs.Policies
{
    public class ShippingServiceDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal Cost { get; set; } = 0;
        public decimal AdditionalItemCost { get; set; } = 0;
        public bool IsFreeShipping { get; set; } = false;
    }

    public class InternationalShippingServiceDto : ShippingServiceDto
    {
        public List<string> ShipToLocations { get; set; } = new List<string>();
    }

    public class ShippingPolicyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int HandlingTimeDays { get; set; }
        public bool IsDefault { get; set; }
        
        // Domestic
        public string DomesticCostType { get; set; } = "Flat";
        public List<ShippingServiceDto> DomesticServices { get; set; } = new();
        
        // International
        public bool IsInternationalShippingAllowed { get; set; } = false;
        public string InternationalCostType { get; set; } = "Flat";
        public List<InternationalShippingServiceDto> InternationalServices { get; set; } = new();
        
        // Preferences
        public List<string> ExcludedLocations { get; set; } = new();

        public byte[]? RowVersion { get; set; }
    }

    public class ReturnPolicyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        public bool IsDomesticAccepted { get; set; }
        public int DomesticReturnDays { get; set; }
        public string DomesticShippingPaidBy { get; set; } = string.Empty;

        public bool IsInternationalAccepted { get; set; }
        public int InternationalReturnDays { get; set; }
        public string InternationalShippingPaidBy { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public byte[]? RowVersion { get; set; }
    }

    public class PaymentPolicyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool ImmediatePaymentRequired { get; set; }
        public int DaysToPayment { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaymentInstructions { get; set; }
        public bool IsDefault { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
