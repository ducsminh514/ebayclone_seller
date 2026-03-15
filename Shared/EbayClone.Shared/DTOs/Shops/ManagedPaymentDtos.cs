using System;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Shops
{
    public class LinkBankAccountRequest
    {
        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 6)]
        public string BankAccountNumber { get; set; } = string.Empty;

        [Required]
        public string BankAccountHolderName { get; set; } = string.Empty;
    }

    public class VerifyMicroDepositRequest
    {
        [Required]
        [Range(0.01, 1.00)]
        public decimal Amount1 { get; set; }

        [Required]
        [Range(0.01, 1.00)]
        public decimal Amount2 { get; set; }
    }

    public class IdentityVerificationRequest
    {
        [Required]
        public string IdentityImageUrl { get; set; } = string.Empty;
    }
}
