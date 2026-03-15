using System.ComponentModel.DataAnnotations;

namespace EbayClone.Shared.DTOs.Shops
{
    /// <summary>
    /// DTO cập nhật Store Profile.
    /// Trên eBay thật: Banner 1280x290px, Logo 300x300px, Description max 1000 ký tự.
    /// </summary>
    public class UpdateShopProfileRequest
    {
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 255 characters.")]
        public string? Name { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// URL ảnh avatar/logo (upload qua FilesController trước, lấy URL truyền vào đây).
        /// eBay thật: 300x300px, max 12MB.
        /// </summary>
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// URL ảnh banner (upload qua FilesController trước, lấy URL truyền vào đây).
        /// eBay thật: 1280x290px, max 12MB.
        /// </summary>
        [StringLength(500)]
        public string? BannerUrl { get; set; }
    }
}
