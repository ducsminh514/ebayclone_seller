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
        public Guid? PaymentPolicyId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }

        // ========== [A2] Condition ở Product level (KHÔNG phải Variant) ==========
        // Values: "New", "New Other", "Open Box", "Seller Refurbished", "Used", "For Parts"
        // Tất cả variants cùng 1 listing phải cùng condition.
        public string Condition { get; set; } = "New";
        public string? ConditionDescription { get; set; } // Mô tả chi tiết (vết xước, thiếu PK...)

        // ========== [A3] Listing Format & Offer ==========
        // "FIXED_PRICE" hoặc "AUCTION"
        // Nếu có Variations → BẮT BUỘC FIXED_PRICE (validate ở Application layer)
        public string ListingFormat { get; set; } = "FIXED_PRICE";
        public bool AllowOffers { get; set; } = false;
        public decimal? AutoAcceptPrice { get; set; }   // Tự chấp nhận offer ≥ X
        public decimal? AutoDeclinePrice { get; set; }  // Tự từ chối offer < Y

        // ========== Status & Scheduling ==========
        // Values: DRAFT, ACTIVE, SCHEDULED, OUT_OF_STOCK, HIDDEN, ENDED
        public string Status { get; set; } = "DRAFT";
        public DateTimeOffset? ScheduledAt { get; set; }
        public decimal? BasePrice { get; set; }
        public string? ReferenceId { get; set; } // SKU or External Reference
        
        // Ảnh sản phẩm: 1 ảnh bìa + danh sách URL (lưu JSON)
        public string? PrimaryImageUrl { get; set; }
        public string? ImageUrls { get; set; } // JSON array: ["url1","url2",...]
        
        // Soft Delete: không bao giờ xóa vật lý để bảo toàn hóa đơn cũ
        public bool IsDeleted { get; set; } = false;
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation properties
        public Shop? Shop { get; set; }
        public Category? Category { get; set; }
        public ShippingPolicy? ShippingPolicy { get; set; }
        public ReturnPolicy? ReturnPolicy { get; set; }
        public PaymentPolicy? PaymentPolicy { get; set; }
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<ProductItemSpecific> ItemSpecifics { get; set; } = new List<ProductItemSpecific>();

        // ========== [A6] Domain Logic: Auto OUT_OF_STOCK ==========
        /// <summary>
        /// Khi tất cả variants hết hàng → tự chuyển OUT_OF_STOCK (giữ SEO ranking).
        /// Khi restock → tự phục hồi ACTIVE.
        /// </summary>
        public void CheckAndUpdateStockStatus()
        {
            if (Status == "ACTIVE" && Variants.Count > 0 && 
                Variants.All(v => v.Quantity - v.ReservedQuantity <= 0))
            {
                Status = "OUT_OF_STOCK";
            }
            else if (Status == "OUT_OF_STOCK" && 
                     Variants.Any(v => v.Quantity - v.ReservedQuantity > 0))
            {
                Status = "ACTIVE";
            }
        }
    }
}
