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

        // Domestic Shipping
        public string DomesticCostType { get; set; } = "Flat"; // Flat, Calculated, Freight, NoShipping
        public string DomesticServicesJson { get; set; } = "[]"; // Serialized JSON array of ShippingServiceDto

        // International Shipping
        public bool IsInternationalShippingAllowed { get; set; } = false;
        public string InternationalCostType { get; set; } = "Flat"; // Flat, Calculated, NoShipping
        public string InternationalServicesJson { get; set; } = "[]"; // Serialized JSON array of InternationalShippingServiceDto

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
