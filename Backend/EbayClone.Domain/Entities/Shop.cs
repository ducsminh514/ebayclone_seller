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

        // Business Policies Opt-in (eBay thật: seller phải chủ động bật trước khi dùng)
        public bool IsPolicyOptedIn { get; set; } = false;

        // ─── Seller Performance Level ───
        // eBay: Below Standard / Above Standard / Top Rated
        // Ảnh hưởng: hold period, listing limit, badge, search ranking
        public string SellerLevel { get; set; } = SellerLevels.NEW;

        // Metrics để tính level (được cập nhật bởi background job hoặc khi order complete)
        public int TotalTransactions { get; set; } = 0;
        public int DefectCount { get; set; } = 0;
        public decimal TotalSalesAmount { get; set; } = 0;
        public DateTimeOffset? LevelEvaluatedAt { get; set; }

        /// <summary>
        /// Hold period (ngày) cho tiền escrow dựa trên seller level.
        /// New seller: 21 ngày, Below Standard: 14 ngày,
        /// Above Standard: 2 ngày, Top Rated: 0 ngày (available ngay).
        /// </summary>
        public int GetHoldDays()
        {
            return SellerLevel switch
            {
                SellerLevels.NEW => 21,
                SellerLevels.BELOW_STANDARD => 14,
                SellerLevels.ABOVE_STANDARD => 2,
                SellerLevels.TOP_RATED => 0,
                _ => 7
            };
        }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public User? Owner { get; set; }
    }
}
