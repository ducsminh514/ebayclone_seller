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
        
        // Inventory
        public int Quantity { get; set; } = 0;
        public int ReservedQuantity { get; set; } = 0;
        
        // Computed stock will be configured in EF Core or readonly property
        public int AvailableStock { get; private set; }
        
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
        /// Domain Logic: Chống bán lố (Overselling)
        /// </summary>
        public void ReserveStock(int quantity)
        {
            if (AvailableStock < quantity)
                throw new InvalidOperationException("Not enough available stock to reserve.");
            ReservedQuantity += quantity;
        }

        public void DeductStockAndReleaseReservation(int quantity)
        {
            if (ReservedQuantity < quantity)
                throw new InvalidOperationException("Cannot deduct more than reserved.");
            if (Quantity < quantity)
                throw new InvalidOperationException("Cannot deduct stock below zero.");
                
            ReservedQuantity -= quantity;
            Quantity -= quantity;
        }

        public void ReleaseReservation(int quantity)
        {
            if (ReservedQuantity < quantity)
                throw new InvalidOperationException("Cannot release more than reserved.");
            ReservedQuantity -= quantity;
        }
    }
}
