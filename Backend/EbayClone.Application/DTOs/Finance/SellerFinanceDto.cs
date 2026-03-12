using System;
using System.Collections.Generic;

namespace EbayClone.Application.DTOs.Finance
{
    public class SellerFinanceDto
    {
        public Guid WalletId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public string Currency { get; set; } = "VND";
        public List<WalletTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // FEE, ESCROW_HOLD, ESCROW_RELEASE
        public string Status { get; set; } = string.Empty; // PENDING, COMPLETED, CANCELLED
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }
        public string? Description { get; set; }
    }
}
