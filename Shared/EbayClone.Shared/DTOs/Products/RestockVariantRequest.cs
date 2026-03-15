using System;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Products
{
    public class RestockVariantRequest
    {
        [Required]
        [Range(1, 100000, ErrorMessage = "Restock quantity must be between 1 and 100,000.")]
        public int AddedQuantity { get; set; }
    }
}
