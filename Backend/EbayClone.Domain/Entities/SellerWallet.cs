using System;

namespace EbayClone.Domain.Entities
{
    public class SellerWallet
    {
        public Guid ShopId { get; set; } // Primary Key
        
        // Virtual Id to satisfy DTO mapping/logic that expects an 'Id'
        public Guid Id => ShopId;
        public string Currency { get; set; } = "VND";

        public decimal AvailableBalance { get; private set; } = 0;
        public decimal PendingBalance { get; private set; } = 0;
        public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        
        // Navigation
        public Shop? Shop { get; set; }

        public void AddPending(decimal amount)
        {
            if (amount < 0) throw new ArgumentException("Amount must be positive.");
            PendingBalance += amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void ReleaseEscrow(decimal pendingDebit, decimal availableCredit)
        {
            if (pendingDebit < 0 || availableCredit < 0) throw new ArgumentException("Amounts must be positive.");
            if (PendingBalance < pendingDebit) throw new InvalidOperationException("Not enough pending balance.");
            if (availableCredit > pendingDebit) throw new ArgumentException("Available credit cannot exceed pending debit.");

            PendingBalance -= pendingDebit;
            AvailableBalance += availableCredit;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Withdraw(decimal amount)
        {
            if (amount < 0) throw new ArgumentException("Amount must be positive.");
            if (AvailableBalance < amount) throw new InvalidOperationException("Not enough available balance.");
            
            AvailableBalance -= amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        
        public void DeductPending(decimal amount) // E.g., for refund before release
        {
            if (amount < 0) throw new ArgumentException("Amount must be positive.");
            if (PendingBalance < amount) throw new InvalidOperationException("Not enough pending balance.");
            
            PendingBalance -= amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
