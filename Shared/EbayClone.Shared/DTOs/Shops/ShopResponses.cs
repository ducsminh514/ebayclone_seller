using System;

namespace EbayClone.Shared.DTOs.Shops
{
    public class ShopCreationResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class VerifyOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class OnboardingStatusResponse
    {
        public bool IsIdentityVerified { get; set; }
        public string? BankVerificationStatus { get; set; }
        public int BankVerificationAttempts { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPolicyOptedIn { get; set; }
        public int MonthlyListingLimit { get; set; }
    }

    public class ShopProfileResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? Address { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
