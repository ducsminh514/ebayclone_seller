using System;

namespace EbayClone.Shared.DTOs.Auth
{
    public class LoginResultDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool HasShop { get; set; }
        public Guid? ShopId { get; set; }
    }
}
