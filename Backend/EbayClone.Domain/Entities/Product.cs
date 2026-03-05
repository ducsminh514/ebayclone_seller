using System;
using System.Collections.Generic;

namespace EbayClone.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string Status { get; set; } = "DRAFT";
        public decimal? BasePrice { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Shop? Shop { get; set; }
        public Category? Category { get; set; }
        public ShippingPolicy? ShippingPolicy { get; set; }
        public ReturnPolicy? ReturnPolicy { get; set; }
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
