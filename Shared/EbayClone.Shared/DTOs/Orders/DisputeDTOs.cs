namespace EbayClone.Shared.DTOs.Orders
{
    // --- Buyer mock: Open Dispute/Case ---
    public class OpenDisputeRequest
    {
        public Guid OrderId { get; set; }
        public string Type { get; set; } = string.Empty;         // "INR" (Item Not Received) | "SNAD" (Significantly Not As Described)
        public string? BuyerMessage { get; set; }
        public string? BuyerEvidenceUrls { get; set; }            // JSON array
    }

    // --- Seller: Respond to Dispute ---
    public class RespondDisputeRequest
    {
        public string SellerMessage { get; set; } = string.Empty;
        public string? SellerEvidenceUrls { get; set; }           // JSON array (tracking proof, photos)

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    // --- Platform: Resolve Dispute ---
    public class ResolveDisputeRequest
    {
        /// <summary>
        /// "BUYER_WIN" | "SELLER_WIN"
        /// </summary>
        public string Resolution { get; set; } = string.Empty;

        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
