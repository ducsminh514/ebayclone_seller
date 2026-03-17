using System;

namespace EbayClone.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        
        // Link to SellerWallet (which uses ShopId as PK)
        public Guid WalletId 
        { 
            get => ShopId; 
            set => ShopId = value; 
        }

        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // 'ORDER_INCOME', 'ESCROW_RELEASE', 'REFUND', 'PLATFORM_FEE', 'WITHDRAW', 'DISPUTE_HOLD'
        public string Status { get; set; } = "COMPLETED"; // 'PENDING', 'COMPLETED', 'CANCELLED'
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        /// <summary>Mã đơn hàng hiển thị (VD: "EB20240317-0001") — để FE show "Đơn #...". Null nếu là WITHDRAW/PAYOUT.</summary>
        public string? OrderNumber { get; set; }
        public string? Description { get; set; }
        
        public decimal BalanceAfter { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        // Navigation
        public Shop? Shop { get; set; }
    }
}
