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
        
        // [A2] Condition ở Product level
        public string Condition { get; set; } = "New";
        public string? ConditionDescription { get; set; }
        
        // [A3] Listing Format & Best Offer
        public string ListingFormat { get; set; } = "FIXED_PRICE";
        public bool AllowOffers { get; set; } = false;
        public decimal? AutoAcceptPrice { get; set; }
        public decimal? AutoDeclinePrice { get; set; }
        public string? Subtitle { get; set; }

        // [A4] Listing Meta
        public bool RequireImmediatePayment { get; set; } = false;
        public bool IsVariationListing { get; set; } = false;

        // [SHIPPING] Package Info
        [System.ComponentModel.DataAnnotations.StringLength(2)]
        public string? CountryOfOrigin { get; set; }
        public decimal? PackageLengthCm { get; set; }
        public decimal? PackageWidthCm { get; set; }
        public decimal? PackageHeightCm { get; set; }

        // Schedule
        public DateTimeOffset? ScheduledAt { get; set; }

        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        public Guid? PaymentPolicyId { get; set; }
        public string Status { get; set; } = "DRAFT";
        
        // Product Images
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }

        // Variants
        [MaxLength(250, ErrorMessage = "A product cannot have more than 250 variants.")]
        public List<UpdateVariantDto> Variants { get; set; } = new();

        // [A5] Item Specifics
        public List<ItemSpecificInput>? ItemSpecifics { get; set; }

        // Concurrency Token
        public byte[]? RowVersion { get; set; }
    }
}
