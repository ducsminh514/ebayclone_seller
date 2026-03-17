namespace EbayClone.Shared.DTOs.Orders
{
    // --- Buyer mock: Open Return Request ---
    public class OpenReturnRequest
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;      // "NOT_AS_DESCRIBED", "DAMAGED", "WRONG_ITEM", "CHANGED_MIND"
        public string? BuyerMessage { get; set; }
        public string? PhotoUrls { get; set; }                    // JSON array
    }

    // --- Seller: Respond to Return ---
    public class RespondReturnRequest
    {
        /// <summary>
        /// "ACCEPT_RETURN" | "PARTIAL_REFUND" | "FULL_REFUND_KEEP_ITEM" | "DECLINE"
        /// </summary>
        public string ResponseType { get; set; } = string.Empty;
        public string? SellerMessage { get; set; }
        
        // Nếu PARTIAL_REFUND: số tiền offer
        public decimal? PartialRefundAmount { get; set; }

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // --- Buyer mock: Ship back ---
    public class ShipReturnRequest
    {
        public string Carrier { get; set; } = string.Empty;
        public string TrackingCode { get; set; } = string.Empty;
    }

    // --- Seller: Issue Refund sau khi nhận hàng ---
    public class IssueRefundRequest
    {
        public decimal RefundAmount { get; set; }
        public decimal DeductionAmount { get; set; }             // max 50% cho damaged (Free Returns)
        public string? DeductionReason { get; set; }
        public bool RestoreStock { get; set; }                    // true nếu hàng return OK

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // --- Buyer mock: Accept/Reject partial refund offer ---
    public class RespondPartialOfferRequest
    {
        /// <summary>"ACCEPT" hoặc "REJECT"</summary>
        public string BuyerDecision { get; set; } = string.Empty;
    }
}
