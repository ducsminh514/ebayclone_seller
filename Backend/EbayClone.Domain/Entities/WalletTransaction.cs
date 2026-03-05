using System;

namespace EbayClone.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ShopId { get; set; }
        
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // 'ORDER_INCOME', 'WITHDRAW', 'REFUND', 'PLATFORM_FEE'
        public Guid? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? Description { get; set; }
        
        public decimal BalanceAfter { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        // Navigation
        public Shop? Shop { get; set; }
    }
}
