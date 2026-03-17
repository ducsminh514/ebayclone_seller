using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Products
{
    public class UpdateProductVariantsRequest
    {
        public List<UpdateVariantDto> Variants { get; set; } = new();
    }

    public class UpdateVariantDto
    {
        public Guid? Id { get; set; } // Nếu NULL thì là tạo mới (optional) - nhưng tập trung vào UPDATE cho an toàn

        [Required(ErrorMessage = "SKU Code is required.")]
        public string SkuCode { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Variant attributes are required.")]
        public Dictionary<string, string> Attributes { get; set; } = new();

        public string? ImageUrl { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Weight must be at least 1 gram.")]
        public int? WeightGram { get; set; }
    }
}
