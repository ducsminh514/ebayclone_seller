using System;

namespace EbayClone.Domain.Entities
{
    public class Voucher
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "PERCENTAGE";
        public decimal Value { get; set; }
        public decimal MinOrderValue { get; set; } = 0;
        public int UsageLimit { get; set; } = 100;
        public int UsedCount { get; set; } = 0;
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Optimistic Concurrency Token
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public Shop? Shop { get; set; }
    }
}
