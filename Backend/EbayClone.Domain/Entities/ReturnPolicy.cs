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

        // International
        public bool IsInternationalAccepted { get; set; } = false;
        public int InternationalReturnDays { get; set; } = 30;
        public string InternationalShippingPaidBy { get; set; } = "BUYER";
        
        public bool IsDefault { get; set; } = false;
 
        // Management
        public bool IsArchived { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Shop? Shop { get; set; }
    }
}
