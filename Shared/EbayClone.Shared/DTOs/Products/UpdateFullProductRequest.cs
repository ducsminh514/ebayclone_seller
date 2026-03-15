using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Products
{
    public class UpdateFullProductRequest
    {
        [Required(ErrorMessage = "Product name is required.")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        public string Status { get; set; } = "DRAFT";
        
        // Product Images
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }

        // Variants
        [MaxLength(50, ErrorMessage = "A product cannot have more than 50 variants.")]
        public List<UpdateVariantDto> Variants { get; set; } = new();

        // Concurrency Token
        public byte[]? RowVersion { get; set; }
    }
}
