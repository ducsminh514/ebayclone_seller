using System;
using System.Collections.Generic;

namespace EbayClone.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OrderNumber { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public Guid BuyerId { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal PlatformFee { get; set; } = 0;
        
        public string Status { get; private set; } = "PENDING_PAYMENT";
        public string PaymentStatus { get; private set; } = "UNPAID";
        
        public string? ShippingCarrier { get; private set; }
        public string? TrackingCode { get; private set; }
        
        // JSON snapshot
        public string? ReceiverInfo { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get; private set; }
        public DateTimeOffset? ShippedAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public Shop? Shop { get; set; }
        public User? Buyer { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        // --- HỆ THỐNG MÁY TRẠNG THÁI (STATE MACHINE) ---

        public void MarkAsPaid()
        {
            if (Status != "PENDING_PAYMENT")
                throw new InvalidOperationException("Chỉ đơn hàng PENDING_PAYMENT mới được phép thanh toán.");
            
            Status = "READY_TO_SHIP";
            PaymentStatus = "PAID";
            PaidAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsPrintedLabel()
        {
            if (Status != "READY_TO_SHIP")
                throw new InvalidOperationException("Đơn hàng phải ở trạng thái đã thanh toán mới được in phiếu gửi.");
            
            Status = "PROCESSING";
        }

        public void MarkAsShipped(string carrier, string trackingCode)
        {
            if (Status != "PROCESSING" && Status != "READY_TO_SHIP")
                throw new InvalidOperationException("Không thể SHIPPED nếu chưa qua bước Chuẩn bị hàng.");

            if (string.IsNullOrWhiteSpace(trackingCode))
                throw new ArgumentException("Mã Tracking không được để trống khi gửi hàng.");

            Status = "SHIPPED";
            ShippingCarrier = carrier;
            TrackingCode = trackingCode;
            ShippedAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsDelivered()
        {
            if (Status != "SHIPPED")
                throw new InvalidOperationException("Đơn hàng chưa được vận chuyển (SHIPPED). Không thể giao thành công.");

            Status = "DELIVERED";
            CompletedAt = DateTimeOffset.UtcNow;
        }

        public void CancelOrder()
        {
            if (Status == "SHIPPED" || Status == "DELIVERED")
                throw new InvalidOperationException("Không thể hủy đơn hàng đã bắt đầu giao.");

            Status = "CANCELLED";
        }

        public void MarkAsCompleted()
        {
            if (Status != "DELIVERED")
                throw new InvalidOperationException("Chỉ đơn hàng đã giao (DELIVERED) mới có thể đánh dấu hoàn tất.");
            
            Status = "COMPLETED";
            IsEscrowReleased = true;
        }

        public bool IsEscrowReleased { get; private set; }
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
