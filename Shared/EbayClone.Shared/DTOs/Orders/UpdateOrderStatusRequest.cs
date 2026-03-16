namespace EbayClone.Shared.DTOs.Orders
{
    public class UpdateOrderStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        
        // SHIPPED fields
        public string? ShippingCarrier { get; set; }
        public string? TrackingCode { get; set; }
        
        // CANCELLED fields
        public string? CancelReason { get; set; }           // "BUYER_ASKED", "OUT_OF_STOCK", "ADDRESS_ISSUE", "BUYER_HASNT_PAID"
        public string? CancelRequestedBy { get; set; }      // "BUYER", "SELLER", "SYSTEM"
        public string? CancelNotes { get; set; }             // Ghi chú thêm khi cancel
        
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
