using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.DTOs.Orders
{
    public class OrderDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public Guid BuyerId { get; set; }

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
        public DateTimeOffset? CompletedAt { get;  set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();


    }
}
