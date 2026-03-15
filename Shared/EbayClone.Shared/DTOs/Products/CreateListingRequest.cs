using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EbayClone.Shared.DTOs.Products
{
    public class CreateListingRequest
    {
        [Required(ErrorMessage = "Category is required.")]
        public Guid CategoryId { get; set; }

        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        public Guid? PaymentPolicyId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(255, MinimumLength = 3, ErrorMessage = "Product name must be between 3 and 255 characters.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        // Ảnh sản phẩm
        [StringLength(500)]
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }

        // Hẹn giờ đăng bán (nếu có, sản phẩm sẽ ở status SCHEDULED)
        public DateTimeOffset? ScheduledAt { get; set; }

        [Required(ErrorMessage = "At least one variant (SKU) is required.")]
        [MinLength(1, ErrorMessage = "At least one variant must be provided.")]
        public List<CreateVariantRequest> Variants { get; set; } = new();
    }

    public class CreateVariantRequest
    {
        [Required(ErrorMessage = "SKU Code is required.")]
        [StringLength(100, ErrorMessage = "SKU Code must not exceed 100 characters.")]
        public string SkuCode { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        // Mảng thuộc tính động dạng Hash (Ví dụ: {"Color": "Red", "Size": "M"})
        [Required(ErrorMessage = "Variant attributes are required.")]
        public Dictionary<string, string> Attributes { get; set; } = new();

        public string? ImageUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Weight must be at least 1 gram.")]
        public int? WeightGram { get; set; }
    }
}
