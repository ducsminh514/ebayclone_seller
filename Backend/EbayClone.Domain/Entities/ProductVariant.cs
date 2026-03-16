using System;
using System.Collections.Generic;

namespace EbayClone.Domain.Entities
{
    public class ProductVariant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        
        public string SkuCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        
        // [A1] Giữ JSON snapshot cho quick read (denormalized)
        // Query/filter dùng AttributeValues relational collection bên dưới
        public string? Attributes { get; set; }
        
        // Inventory — single-step deduction (không dùng ReservedQuantity nữa)
        public int Quantity { get; set; } = 0;
        
        public string? ImageUrl { get; set; }
        public int? WeightGram { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string? LastModifiedBy { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Product? Product { get; set; }
        public ICollection<VariantAttributeValue> AttributeValues { get; set; } = new List<VariantAttributeValue>();
        
        /// <summary>
        /// Domain Logic: Trừ kho trực tiếp khi đơn hàng được thanh toán (1 bước duy nhất).
        /// Dùng với ExecuteUpdateAsync ở Repository level để đảm bảo atomic + chống race condition.
        /// </summary>
        public void DeductStock(int quantity)
        {
            if (Quantity < quantity)
                throw new InvalidOperationException($"Không đủ hàng tồn kho. Hiện có: {Quantity}, yêu cầu: {quantity}.");
            Quantity -= quantity;
        }

        /// <summary>
        /// Domain Logic: Hoàn kho khi cancel/return.
        /// </summary>
        public void RestoreStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Số lượng hoàn phải > 0.");
            Quantity += quantity;
        }
    }
}
