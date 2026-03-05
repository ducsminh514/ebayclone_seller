using System;
using System.Collections.Generic;

namespace EbayClone.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public Guid BuyerId { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal PlatformFee { get; set; } = 0;
        
        public string Status { get; set; } = "PENDING_PAYMENT";
        public string PaymentStatus { get; set; } = "UNPAID";
        
        public string? ShippingCarrier { get; set; }
        public string? TrackingCode { get; set; }
        
        // JSON snapshot
        public string? ReceiverInfo { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset? ShippedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        // Navigation
        public Shop? Shop { get; set; }
        public User? Buyer { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
    
    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        
        public string? ProductNameSnapshot { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
        
        public decimal TotalLineAmount { get; private set; }

        // Navigation
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public ProductVariant? Variant { get; set; }
    }
}
