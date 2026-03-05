using System;

namespace EbayClone.Domain.Entities
{
    public class ShippingPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int HandlingTimeDays { get; set; } = 2;
        public decimal Cost { get; set; } = 0;
        public bool IsDefault { get; set; } = false;

        // Navigation
        public Shop? Shop { get; set; }
    }
}
