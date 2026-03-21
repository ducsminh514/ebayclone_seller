using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Quản lý yêu cầu trả hàng/hoàn tiền.
    /// Luồng: Buyer request → Seller respond (3 ngày) → Buyer ship back → Seller inspect → Refund
    /// </summary>
    public class OrderReturn
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        
        // Return Request
        public string Reason { get; set; } = string.Empty;     // "NOT_AS_DESCRIBED", "DAMAGED", "WRONG_ITEM", "CHANGED_MIND"
        public string? BuyerMessage { get; set; }
        public string? PhotoUrls { get; set; }                  // JSON array

        // Seller Response
        // Status: REQUESTED → ACCEPTED/PARTIAL_OFFERED/DECLINED → IN_PROGRESS → REFUNDED/PARTIALLY_REFUNDED/CLOSED
        public string Status { get; private set; } = "REQUESTED";
        public string? SellerResponseType { get; private set; }         // "ACCEPT_RETURN", "PARTIAL_REFUND", "FULL_REFUND_KEEP_ITEM", "DECLINE"
        public decimal? PartialOfferAmount { get; private set; }         // Số tiền seller offer (khi PARTIAL_OFFERED)
        public string? SellerMessage { get; set; }

        // Refund
        public decimal? RefundAmount { get; private set; }
        public decimal? DeductionAmount { get; private set; }           // max 50% for damaged returns
        public string? DeductionReason { get; set; }

        // Return Shipping
        public string? ReturnTrackingCode { get; private set; }
        public string? ReturnCarrier { get; private set; }
        public string ReturnShippingPaidBy { get; set; } = "BUYER";  // "BUYER" or "SELLER" (SNAD = SELLER)
        // [FIX-H6] Chi phí ship trả hàng — ghi nhận để tracking financial
        public decimal ReturnShippingCost { get; set; } = 0;

        // Stock
        public bool IsStockRestored { get; set; }

        // Timestamps
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? RespondedAt { get; private set; }
        public DateTimeOffset? ReturnShippedAt { get; private set; }
        public DateTimeOffset? ReturnReceivedAt { get; private set; }
        public DateTimeOffset? RefundedAt { get; private set; }
        public DateTimeOffset SellerResponseDeadline { get; set; }  // Auto-set: RequestedAt + 3 days

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public User? Buyer { get; set; }

        // --- DOMAIN METHODS ---

        /// <summary>
        /// Auto-set deadline khi tạo return request.
        /// Gọi sau khi khởi tạo entity.
        /// </summary>
        public void InitializeDeadline()
        {
            SellerResponseDeadline = RequestedAt.AddDays(3);
        }

        public void AcceptReturn(string responseType, string? message = null)
        {
            if (Status != "REQUESTED")
                throw new InvalidOperationException("Chỉ return REQUESTED mới được chấp nhận.");
            
            Status = "ACCEPTED";
            SellerResponseType = responseType; // "ACCEPT_RETURN", "FULL_REFUND_KEEP_ITEM"
            SellerMessage = message;
            RespondedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Seller offer partial refund → chờ buyer accept/reject.
        /// Chưa trừ tiền — chỉ khi buyer ACCEPT mới thực hiện refund.
        /// </summary>
        public void OfferPartialRefund(decimal amount, string? message = null)
        {
            if (Status != "REQUESTED")
                throw new InvalidOperationException("Chỉ return REQUESTED mới được offer.");
            if (amount <= 0)
                throw new ArgumentException("Số tiền offer phải > 0.");
            
            Status = "PARTIAL_OFFERED";
            SellerResponseType = "PARTIAL_REFUND";
            PartialOfferAmount = amount;
            SellerMessage = message;
            RespondedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Buyer accept partial offer → thực hiện refund.
        /// </summary>
        public void AcceptPartialOffer()
        {
            if (Status != "PARTIAL_OFFERED")
                throw new InvalidOperationException("Chỉ PARTIAL_OFFERED mới được buyer accept.");
            if (!PartialOfferAmount.HasValue || PartialOfferAmount.Value <= 0)
                throw new InvalidOperationException("Không có offer amount.");
            
            // Chuyển sang ACCEPTED để MarkRefunded() cho phép tiếp tục
            Status = "ACCEPTED";
        }

        /// <summary>
        /// Buyer reject partial offer → quay lại REQUESTED (seller có thể offer lại hoặc buyer escalate).
        /// </summary>
        public void RejectPartialOffer()
        {
            if (Status != "PARTIAL_OFFERED")
                throw new InvalidOperationException("Chỉ PARTIAL_OFFERED mới được buyer reject.");
            
            Status = "REQUESTED";
            PartialOfferAmount = null;
            SellerResponseType = null;
            SellerMessage = null;
            RespondedAt = null;
        }

        public void DeclineReturn(string? message = null)
        {
            if (Status != "REQUESTED")
                throw new InvalidOperationException("Chỉ return REQUESTED mới được từ chối.");
            
            Status = "DECLINED";
            SellerResponseType = "DECLINE";
            SellerMessage = message;
            RespondedAt = DateTimeOffset.UtcNow;
        }

        public void MarkReturnShipped(string carrier, string trackingCode)
        {
            if (Status != "ACCEPTED")
                throw new InvalidOperationException("Return phải được chấp nhận trước khi buyer gửi hàng lại.");
            
            Status = "IN_PROGRESS";
            ReturnCarrier = carrier;
            ReturnTrackingCode = trackingCode;
            ReturnShippedAt = DateTimeOffset.UtcNow;
        }

        public void MarkReturnReceived()
        {
            if (Status != "IN_PROGRESS")
                throw new InvalidOperationException("Hàng phải đang trong quá trình gửi lại.");
            ReturnReceivedAt = DateTimeOffset.UtcNow;
        }

        public void MarkRefunded(decimal refundAmount, decimal deductionAmount = 0)
        {
            if (Status != "IN_PROGRESS" && Status != "ACCEPTED")
                throw new InvalidOperationException("Không thể hoàn tiền ở trạng thái hiện tại.");
            
            Status = deductionAmount > 0 ? "PARTIALLY_REFUNDED" : "REFUNDED";
            RefundAmount = refundAmount;
            DeductionAmount = deductionAmount;
            RefundedAt = DateTimeOffset.UtcNow;
        }
    }
}
