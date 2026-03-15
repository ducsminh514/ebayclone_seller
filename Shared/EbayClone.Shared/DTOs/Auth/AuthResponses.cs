namespace EbayClone.Shared.DTOs.Auth
{
    public class AuthResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
