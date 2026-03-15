using System;
using System.Collections.Generic;

namespace EbayClone.Shared.DTOs.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string Status { get; set; } = "DRAFT";
        public DateTimeOffset? ScheduledAt { get; set; }
        public decimal? BasePrice { get; set; }
        
        public string? PrimaryImageUrl { get; set; }
        public string? ImageUrls { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public byte[]? RowVersion { get; set; }

        public ICollection<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }

    public class ProductVariantDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string SkuCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Attributes { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public int? WeightGram { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
