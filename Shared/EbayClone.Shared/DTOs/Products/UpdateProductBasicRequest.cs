using System;

namespace EbayClone.Shared.DTOs.Products
{
    public class UpdateProductBasicRequest
    {
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
        
        public Guid CategoryId { get; set; }
        public Guid? ShippingPolicyId { get; set; }
        public Guid? ReturnPolicyId { get; set; }
        public string Status { get; set; } = "DRAFT";
        
        // Ảnh sản phẩm
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}
