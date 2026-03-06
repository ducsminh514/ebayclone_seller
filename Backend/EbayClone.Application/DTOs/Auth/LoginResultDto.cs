using System;

namespace EbayClone.Application.DTOs.Auth
{
    public class LoginResultDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool HasShop { get; set; }
        public Guid? ShopId { get; set; }
    }
}
