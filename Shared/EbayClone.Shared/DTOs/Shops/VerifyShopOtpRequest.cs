using System;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Shops
{
    public class VerifyShopOtpRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có đúng 6 số")]
        public string OtpCode { get; set; } = string.Empty;
    }
}
