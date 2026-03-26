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
        // [eBay Rule] Title tối đa 80 ký tự (bao gồm dấu cách) — chuẩn eBay Cassini
        [StringLength(80, MinimumLength = 3, ErrorMessage = "Tiêu đề phải từ 3 đến 80 ký tự.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        // [A2] Condition ở Product level — tất cả variants cùng 1 condition
        // Values: "New", "New Other", "Open Box", "Seller Refurbished", "Used", "For Parts"
        [StringLength(50)]
        public string Condition { get; set; } = "New";
        
        [StringLength(500)]
        public string? ConditionDescription { get; set; }

        // [A3] Listing Format & Best Offer
        // "FIXED_PRICE" hoặc "AUCTION". Nếu có > 1 variant → BẮT BUỘC FIXED_PRICE.
        [StringLength(20)]
        public string ListingFormat { get; set; } = "FIXED_PRICE";
        public bool AllowOffers { get; set; } = false;
        public decimal? AutoAcceptPrice { get; set; }
        public decimal? AutoDeclinePrice { get; set; }

        // [A4] Listing Meta
        public bool RequireImmediatePayment { get; set; } = false;
        // IsVariationListing: true = multi-SKU listing, false = single-item (Price+Qty ở PRICING level)
        public bool IsVariationListing { get; set; } = false;

        // [SHIPPING] Package Info
        // CountryOfOrigin: ISO 3166-1 alpha-2 (VD: "VN", "CN", "US")
        [StringLength(2)]
        public string? CountryOfOrigin { get; set; }
        public decimal? PackageLengthCm { get; set; }
        public decimal? PackageWidthCm { get; set; }
        public decimal? PackageHeightCm { get; set; }
        
        // [eBay Rule] Subtitle tối đa 55 ký tự — eBay charge phí $1.50-$3.00/lần list
        [StringLength(55, ErrorMessage = "Phụ đề (Subtitle) không được vượt quá 55 ký tự.")]
        public string? Subtitle { get; set; }

        // Ảnh sản phẩm
        [StringLength(500)]
        public string? PrimaryImageUrl { get; set; }
        public List<string>? ImageUrls { get; set; }

        // Hẹn giờ đăng bán (nếu có, sản phẩm sẽ ở status SCHEDULED)
        public DateTimeOffset? ScheduledAt { get; set; }

        [Required(ErrorMessage = "At least one variant (SKU) is required.")]
        [MinLength(1, ErrorMessage = "At least one variant must be provided.")]
        public List<CreateVariantRequest> Variants { get; set; } = new();

        // [A5] Item Specifics — seller nhập giá trị cho thuộc tính category yêu cầu
        public List<ItemSpecificInput>? ItemSpecifics { get; set; }
    }

    public class ItemSpecificInput
    {
        [Required]
        public string Name { get; set; } = string.Empty;  // "Brand", "Model"
        [Required]
        public string Value { get; set; } = string.Empty;  // "Apple", "iPhone 15"
    }

    public class CreateVariantRequest
    {
        [Required(ErrorMessage = "SKU Code is required.")]
        [StringLength(50, ErrorMessage = "SKU Code must not exceed 50 characters (eBay API limit).")]
        public string SkuCode { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }

        // [NOTE] Attributes không dùng [Required] ở DTO — UseCase xử lý business rule này
        // DataAnnotationsValidator cũng không validate nested collection items nên [Required] ở đây vô tác dụng
        public Dictionary<string, string> Attributes { get; set; } = new();

        public string? ImageUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Weight must be at least 1 gram.")]
        public int? WeightGram { get; set; }
    }
}
