using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Quản lý tranh chấp (Case) giữa buyer và seller.
    /// Types: INR (Item Not Received), SNAD (Significantly Not As Described)
    /// Luồng: Buyer open → Seller respond (3 days) → Escalate (optional) → Platform resolve
    /// </summary>
    public class OrderDispute
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }

        public string Type { get; set; } = string.Empty;         // "INR", "SNAD"
        public string? BuyerMessage { get; set; }
        public string? BuyerEvidenceUrls { get; set; }           // JSON array

        // Seller Response
        public string? SellerMessage { get; private set; }
        public string? SellerEvidenceUrls { get; private set; }          // JSON array (tracking proof, photos)

        // Status: OPENED → SELLER_RESPONDED → ESCALATED → RESOLVED_BUYER_WIN/RESOLVED_SELLER_WIN
        public string Status { get; private set; } = "OPENED";
        public string? Resolution { get; private set; }                   // "FULL_REFUND", "NO_REFUND"

        // Impact
        public bool IsDefect { get; private set; }                       // true nếu buyer win

        // Timestamps
        public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? SellerRespondedAt { get; private set; }
        public DateTimeOffset? EscalatedAt { get; private set; }
        public DateTimeOffset? ResolvedAt { get; private set; }
        public DateTimeOffset SellerResponseDeadline { get; set; }  // Auto-set: OpenedAt + 3 days

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        // Navigation
        public Order? Order { get; set; }
        public User? Buyer { get; set; }

        // --- DOMAIN METHODS ---

        /// <summary>
        /// Auto-set deadline khi tạo dispute.
        /// </summary>
        public void InitializeDeadline()
        {
            SellerResponseDeadline = OpenedAt.AddDays(3);
        }

        public void SellerRespond(string message, string? evidenceUrls = null)
        {
            if (Status != "OPENED")
                throw new InvalidOperationException("Dispute phải ở trạng thái OPENED để seller phản hồi.");

            Status = "SELLER_RESPONDED";
            SellerMessage = message;
            SellerEvidenceUrls = evidenceUrls;
            SellerRespondedAt = DateTimeOffset.UtcNow;
        }

        public void Escalate()
        {
            if (Status != "OPENED" && Status != "SELLER_RESPONDED")
                throw new InvalidOperationException("Chỉ dispute OPENED hoặc SELLER_RESPONDED mới có thể escalate.");

            Status = "ESCALATED";
            EscalatedAt = DateTimeOffset.UtcNow;
        }

        public void ResolveBuyerWin()
        {
            if (Status != "ESCALATED" && Status != "SELLER_RESPONDED" && Status != "OPENED")
                throw new InvalidOperationException("Dispute phải được escalate hoặc đang mở để resolve.");

            Status = "RESOLVED_BUYER_WIN";
            Resolution = "FULL_REFUND";
            IsDefect = true; // eBay rule: buyer win = seller defect
            ResolvedAt = DateTimeOffset.UtcNow;
        }

        public void ResolveSellerWin()
        {
            if (Status != "ESCALATED" && Status != "SELLER_RESPONDED" && Status != "OPENED")
                throw new InvalidOperationException("Dispute phải được escalate hoặc đang mở để resolve.");

            Status = "RESOLVED_SELLER_WIN";
            Resolution = "NO_REFUND";
            IsDefect = false;
            ResolvedAt = DateTimeOffset.UtcNow;
        }
    }
}
