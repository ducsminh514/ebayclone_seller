using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Quản lý yêu cầu hủy đơn + lý do + impact đến seller metrics.
    /// eBay rule: Seller cancel vì OOS = defect, Buyer cancel = không defect
    /// </summary>
    public class OrderCancellation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }

        public string RequestedBy { get; set; } = string.Empty;  // "BUYER", "SELLER", "SYSTEM"
        public string Reason { get; set; } = string.Empty;       // "BUYER_ASKED", "BUYER_HASNT_PAID", "OUT_OF_STOCK", "ADDRESS_ISSUE"
        public string? Notes { get; set; }

        // Status: REQUESTED → ACCEPTED/DECLINED → COMPLETED
        public string Status { get; private set; } = "REQUESTED";

        // Impact on Seller Metrics
        public bool IsDefect { get; private set; }                       // true nếu seller cancel vì OUT_OF_STOCK
        public bool IsFeeCredited { get; private set; }                  // true nếu buyer unpaid cancel → hoàn phí
        public bool IsStockRestored { get; set; }

        // Timestamps
        public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? RespondedAt { get; private set; }
        public DateTimeOffset ResponseDeadline { get; set; }     // Auto-set: RequestedAt + 3 days

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Order? Order { get; set; }

        // --- DOMAIN METHODS ---

        /// <summary>
        /// Auto-set deadline + auto-detect defect/fee credit khi tạo cancellation request.
        /// </summary>
        public void Initialize()
        {
            ResponseDeadline = RequestedAt.AddDays(3);
            // eBay rule: seller cancel vì OOS = defect
            IsDefect = (RequestedBy == "SELLER" && Reason == "OUT_OF_STOCK");
            // Buyer chưa thanh toán → seller được hoàn phí
            IsFeeCredited = (Reason == "BUYER_HASNT_PAID");
        }

        public void Accept()
        {
            if (Status != "REQUESTED")
                throw new InvalidOperationException("Chỉ cancel request REQUESTED mới được chấp nhận.");
            Status = "ACCEPTED";
            RespondedAt = DateTimeOffset.UtcNow;
        }

        public void Decline(string? declineNotes = null)
        {
            if (Status != "REQUESTED")
                throw new InvalidOperationException("Chỉ cancel request REQUESTED mới được từ chối.");
            Status = "DECLINED";
            Notes = declineNotes ?? Notes;
            RespondedAt = DateTimeOffset.UtcNow;
        }

        public void MarkCompleted()
        {
            if (Status != "ACCEPTED")
                throw new InvalidOperationException("Cancel phải được chấp nhận trước khi hoàn tất.");
            Status = "COMPLETED";
        }
    }
}
