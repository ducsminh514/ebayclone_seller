using System;

namespace EbayClone.Shared.DTOs.Orders
{
    public class CreateBuyerTestOrderRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string ReceiverInfo { get; set; } = string.Empty;
    }
}
