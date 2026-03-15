using System;

namespace EbayClone.Domain.Entities
{
    public class Shop
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? TaxCode { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public decimal RatingAvg { get; set; } = 0;
        public int TotalShippingPolicies { get; set; } = 0;
        public int TotalReturnPolicies { get; set; } = 0;
        public int TotalPaymentPolicies { get; set; } = 0;
        // Giới hạn đăng bài mặc định: 10 SP/tháng cho seller mới (nâng khi lên cấp)
        public int MonthlyListingLimit { get; set; } = 250;
        
        // Identity & Verification (KYC)
        public string? IdentityImageUrl { get; set; }
        public bool IsIdentityVerified { get; set; } = false;

        // Managed Payments (Payouts)
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountHolderName { get; set; }
        public string? BankVerificationStatus { get; set; } = "NotStarted"; // NotStarted, Pending, Verified, Failed
        public decimal MicroDepositAmount1 { get; set; } = 0;
        public decimal MicroDepositAmount2 { get; set; } = 0;
        public int BankVerificationAttempts { get; set; } = 0; // Chặn brute-force (tối đa 3 lần)

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public User? Owner { get; set; }
    }
}
