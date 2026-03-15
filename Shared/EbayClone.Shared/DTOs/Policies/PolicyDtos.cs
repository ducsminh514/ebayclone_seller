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

        // Free Shipping
        public bool OfferFreeShipping { get; set; }
        
        // Domestic
        public string DomesticCostType { get; set; } = "Flat";
        public List<ShippingServiceDto> DomesticServices { get; set; } = new();
        
        // International
        public bool IsInternationalShippingAllowed { get; set; } = false;
        public string InternationalCostType { get; set; } = "Flat";
        public List<InternationalShippingServiceDto> InternationalServices { get; set; } = new();

        // Combined Shipping
        public bool OfferCombinedShippingDiscount { get; set; }

        // Package Details
        public string PackageType { get; set; } = "Package";
        public decimal PackageWeightOz { get; set; }
        public string PackageDimensionsJson { get; set; } = "{}";

        // Handling Cutoff
        public string HandlingTimeCutoff { get; set; } = "14:00";
        
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

        public bool AutoAcceptReturns { get; set; }
        public bool SendImmediateRefund { get; set; }
        public string? ReturnAddressJson { get; set; }
        public decimal RestockingFeePercent { get; set; }

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
