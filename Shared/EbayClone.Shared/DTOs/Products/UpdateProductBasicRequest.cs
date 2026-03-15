using System;

namespace EbayClone.Shared.DTOs.Products
{
    public class UpdateProductBasicRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        public string Status { get; set; } = "DRAFT";
        
        // Ảnh sản phẩm
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}
