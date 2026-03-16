using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Shared.DTOs.Products;

namespace EbayClone.Shared.DTOs.Orders
{
    public class OrderDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public Guid BuyerId { get; set; }
        public string? BuyerName { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal PlatformFee { get; set; } = 0;

        public string Status { get;  set; } = "PENDING_PAYMENT";
        public string PaymentStatus { get;  set; } = "UNPAID";

        public string? ShippingCarrier { get;  set; }
        public string? TrackingCode { get;  set; }

        // JSON snapshot
        public string? ReceiverInfo { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get;  set; }
        public DateTimeOffset? ShippedAt { get;  set; }
        public DateTimeOffset? DeliveredAt { get; set; }
        public DateTimeOffset? CompletedAt { get;  set; }
        public DateTimeOffset? CancelledAt { get; set; }
        
        public DateTimeOffset? ShipByDate { get; set; }
        public DateTimeOffset? ReturnDeadline { get; set; }
        public string? CancelReason { get; set; }
        public string? CancelRequestedBy { get; set; }
        
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public bool IsEscrowReleased { get; set; }
 
        public ICollection<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }

    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string? ProductNameSnapshot { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public decimal TotalLineAmount { get; set; }
        public ProductVariantDto? Variant { get; set; }
    }
}
