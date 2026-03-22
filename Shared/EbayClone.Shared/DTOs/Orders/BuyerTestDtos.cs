using System;

namespace EbayClone.Shared.DTOs.Orders
{
    public class CreateBuyerTestOrderRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string ReceiverInfo { get; set; } = string.Empty;
        /// <summary>Mã giảm giá (optional). Null = không dùng voucher.</summary>
        public string? VoucherCode { get; set; }
    }

    public class BuyerCancelRequest
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = "BUYER_ASKED";
    }

    public class RespondCancelRequest
    {
        public bool Accept { get; set; }
        public string? DeclineNotes { get; set; }
    }
}
