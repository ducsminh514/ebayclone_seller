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
        
        // --- STATUS ---
        // Valid: PENDING_PAYMENT, PAID, SHIPPED, DELIVERED, COMPLETED,
        //        CANCELLED, RETURN_REQUESTED, RETURN_IN_PROGRESS, 
        //        REFUNDED, PARTIALLY_REFUNDED, DISPUTE_OPENED
        public string Status { get; private set; } = "PENDING_PAYMENT";
        public string PaymentStatus { get; private set; } = "UNPAID";
        
        public string? ShippingCarrier { get; private set; }
        public string? TrackingCode { get; private set; }
        
        // JSON snapshot
        public string? ReceiverInfo { get; set; }

        // --- TIMESTAMPS ---
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get; private set; }
        public DateTimeOffset? ShippedAt { get; private set; }
        public DateTimeOffset? DeliveredAt { get; private set; }
        public DateTimeOffset? CompletedAt { get; private set; }
        public DateTimeOffset? CancelledAt { get; private set; }
        
        // --- ORDER FLOW FIELDS ---
        public DateTimeOffset? ShipByDate { get; private set; }           // PaidAt + HandlingTimeDays
        public DateTimeOffset? ReturnDeadline { get; private set; }       // DeliveredAt + ReturnPolicy.ReturnDays
        public string? CancelReason { get; private set; }         // "BUYER_ASKED", "OUT_OF_STOCK", "ADDRESS_ISSUE", "BUYER_HASNT_PAID"
        public string? CancelRequestedBy { get; private set; }    // "BUYER", "SELLER", "SYSTEM"
        public bool IsEscrowReleased { get; private set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public Shop? Shop { get; set; }
        public User? Buyer { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderReturn> Returns { get; set; } = new List<OrderReturn>();
        public ICollection<OrderCancellation> Cancellations { get; set; } = new List<OrderCancellation>();
        public ICollection<OrderDispute> Disputes { get; set; } = new List<OrderDispute>();

        // --- HỆ THỐNG MÁY TRẠNG THÁI (STATE MACHINE) ---
        // Luồng chính: PENDING_PAYMENT → PAID → SHIPPED → DELIVERED → COMPLETED
        // Luồng phụ:   DELIVERED → RETURN_REQUESTED → RETURN_IN_PROGRESS → REFUNDED/PARTIALLY_REFUNDED
        //              DELIVERED → DISPUTE_OPENED → REFUNDED (buyer win) / COMPLETED (seller win)
        //              PENDING_PAYMENT/PAID → CANCELLED

        public void MarkAsPaid()
        {
            if (Status != "PENDING_PAYMENT")
                throw new InvalidOperationException("Chỉ đơn hàng PENDING_PAYMENT mới được phép thanh toán.");
            
            Status = "PAID";
            PaymentStatus = "PAID";
            PaidAt = DateTimeOffset.UtcNow;
            // ShipByDate sẽ được set ở UseCase level vì cần truy cập ShippingPolicy.HandlingTimeDays
        }

        public void MarkAsShipped(string carrier, string trackingCode)
        {
            if (Status != "PAID")
                throw new InvalidOperationException("Chỉ đơn hàng đã thanh toán (PAID) mới được phép gửi hàng.");

            if (string.IsNullOrWhiteSpace(trackingCode))
                throw new ArgumentException("Mã Tracking không được để trống khi gửi hàng.");

            Status = "SHIPPED";
            ShippingCarrier = carrier;
            TrackingCode = trackingCode;
            ShippedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Set deadline giao hàng (gọi sau MarkAsPaid, ở UseCase level khi có access ShippingPolicy).
        /// </summary>
        public void SetShipByDate(int handlingTimeDays)
        {
            if (!PaidAt.HasValue)
                throw new InvalidOperationException("Phải thanh toán trước khi set deadline giao hàng.");
            ShipByDate = PaidAt.Value.AddDays(handlingTimeDays);
        }

        public void MarkAsDelivered()
        {
            if (Status != "SHIPPED")
                throw new InvalidOperationException("Đơn hàng chưa được vận chuyển (SHIPPED). Không thể giao thành công.");

            Status = "DELIVERED";
            DeliveredAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Set deadline trả hàng (gọi sau MarkAsDelivered, ở UseCase level khi có access ReturnPolicy).
        /// </summary>
        public void SetReturnDeadline(int returnDays)
        {
            if (!DeliveredAt.HasValue)
                throw new InvalidOperationException("Phải giao hàng trước khi set deadline trả hàng.");
            ReturnDeadline = DeliveredAt.Value.AddDays(returnDays);
        }

        public void CancelOrder(string reason, string requestedBy)
        {
            // Whitelist: chỉ cho phép cancel khi chưa giao hàng
            if (Status != "PENDING_PAYMENT" && Status != "PAID")
                throw new InvalidOperationException(
                    $"Không thể hủy đơn hàng ở trạng thái '{Status}'. Chỉ đơn PENDING_PAYMENT hoặc PAID mới được hủy.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hủy đơn không được để trống.");
            if (string.IsNullOrWhiteSpace(requestedBy))
                throw new ArgumentException("Người yêu cầu hủy không được để trống.");

            Status = "CANCELLED";
            CancelReason = reason;
            CancelRequestedBy = requestedBy;
            CancelledAt = DateTimeOffset.UtcNow;
        }

        public void MarkAsCompleted()
        {
            if (Status != "DELIVERED" && Status != "DISPUTE_OPENED")
                throw new InvalidOperationException("Chỉ đơn hàng DELIVERED hoặc DISPUTE_OPENED mới có thể đánh dấu hoàn tất.");
            
            Status = "COMPLETED";
            CompletedAt = DateTimeOffset.UtcNow;
            IsEscrowReleased = true;
        }

        // --- RETURN FLOW ---
        public void MarkAsReturnRequested()
        {
            if (Status != "DELIVERED")
                throw new InvalidOperationException("Chỉ đơn hàng đã giao (DELIVERED) mới có thể yêu cầu trả hàng.");
            Status = "RETURN_REQUESTED";
        }

        public void MarkAsReturnInProgress()
        {
            if (Status != "RETURN_REQUESTED")
                throw new InvalidOperationException("Phải có yêu cầu trả hàng trước khi xử lý.");
            Status = "RETURN_IN_PROGRESS";
        }

        public void MarkAsRefunded()
        {
            if (Status != "RETURN_IN_PROGRESS" && Status != "RETURN_REQUESTED" && Status != "DISPUTE_OPENED")
                throw new InvalidOperationException("Không thể hoàn tiền ở trạng thái hiện tại.");
            Status = "REFUNDED";
        }

        public void MarkAsPartiallyRefunded()
        {
            if (Status != "RETURN_IN_PROGRESS" && Status != "RETURN_REQUESTED" && Status != "DISPUTE_OPENED")
                throw new InvalidOperationException("Không thể hoàn tiền một phần ở trạng thái hiện tại.");
            Status = "PARTIALLY_REFUNDED";
        }

        // --- DISPUTE FLOW ---
        public void MarkAsDisputeOpened()
        {
            if (Status != "DELIVERED" && Status != "RETURN_REQUESTED")
                throw new InvalidOperationException("Chỉ đơn hàng DELIVERED hoặc RETURN_REQUESTED mới có thể mở tranh chấp.");
            Status = "DISPUTE_OPENED";
        }
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
