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
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string Status { get; set; } = "DRAFT";
        // Thời điểm hẹn giờ đăng bán (chỉ dùng khi Status = SCHEDULED)
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
    }
}
