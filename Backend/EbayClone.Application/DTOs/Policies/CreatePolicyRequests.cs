using System.ComponentModel.DataAnnotations;

namespace EbayClone.Application.DTOs.Policies
{
    public class CreateShippingPolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 30, ErrorMessage = "Thời gian chuẩn bị hàng tối đa 30 ngày")]
        public int HandlingTimeDays { get; set; } = 2;

        [Range(0, 100000000, ErrorMessage = "Chi phí ship không hợp lệ")]
        public decimal Cost { get; set; } = 0;

        public bool IsDefault { get; set; } = false;
    }

    public class CreateReturnPolicyRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int ReturnDays { get; set; } = 30;

        [RegularExpression("BUYER|SELLER", ErrorMessage = "Chỉ chấp nhận BUYER hoặc SELLER")]
        public string ShippingPaidBy { get; set; } = "BUYER";
    }
}
