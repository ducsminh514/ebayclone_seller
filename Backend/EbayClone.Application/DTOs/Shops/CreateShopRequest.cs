using System;
using System.ComponentModel.DataAnnotations;

namespace EbayClone.Application.DTOs.Shops
{
    public class CreateShopRequest
    {
        [Required(ErrorMessage = "Tên Shop bắt buộc nhập")]
        [MinLength(3, ErrorMessage = "Tên Shop phải có ít nhất 3 ký tự")]
        [MaxLength(100, ErrorMessage = "Tên Shop không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [MaxLength(20, ErrorMessage = "Mã số thuế không được vượt quá 20 ký tự")]
        public string? TaxCode { get; set; }

        [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string? Address { get; set; }
    }
}
