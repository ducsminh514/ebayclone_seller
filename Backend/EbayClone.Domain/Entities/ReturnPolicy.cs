using System;

namespace EbayClone.Domain.Entities
{
    public class ReturnPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ReturnDays { get; set; } = 0; // 0 = No Returns
        public string ShippingPaidBy { get; set; } = "BUYER"; // 'BUYER' or 'SELLER'

        // Navigation
        public Shop? Shop { get; set; }
    }
}
