using System;

namespace EbayClone.Domain.Entities
{
    public class ShippingPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int HandlingTimeDays { get; set; } = 2; // e.g., Same day = 0, 1 = 1 day
        public bool IsDefault { get; set; } = false;

        // Free Shipping (eBay: toggle nổi bật trên form tạo policy)
        public bool OfferFreeShipping { get; set; } = false;

        // Domestic Shipping
        public string DomesticCostType { get; set; } = "Flat"; // Flat, Calculated, Freight, NoShipping
        public string DomesticServicesJson { get; set; } = "[]"; // Serialized JSON array of ShippingServiceDto

        // International Shipping
        public bool IsInternationalShippingAllowed { get; set; } = false;
        public string InternationalCostType { get; set; } = "Flat"; // Flat, Calculated, NoShipping
        public string InternationalServicesJson { get; set; } = "[]"; // Serialized JSON array of InternationalShippingServiceDto

        // Combined Shipping Discount (eBay: giảm giá ship khi buyer mua nhiều item cùng seller)
        public bool OfferCombinedShippingDiscount { get; set; } = false;

        // Package Details (eBay: weight/dimensions template cho listings dùng policy này)
        public string PackageType { get; set; } = "Package"; // Package, LargePackage, Letter, LargeEnvelope
        public decimal PackageWeightOz { get; set; } = 0; // Ounces
        public string PackageDimensionsJson { get; set; } = "{}"; // { length, width, height in inches }

        // Handling Time Cutoff (eBay: giờ cutoff, sau giờ này → cộng thêm 1 ngày handling)
        public string HandlingTimeCutoff { get; set; } = "14:00"; // 24h format

        // Preferences
        public string ExcludedLocationsJson { get; set; } = "[]"; // Serialized JSON array of strings

        // Management
        public bool IsArchived { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Shop? Shop { get; set; }
    }
}
