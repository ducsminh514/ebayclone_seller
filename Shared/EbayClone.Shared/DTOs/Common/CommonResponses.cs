namespace EbayClone.Shared.DTOs.Common
{
    public class SuccessResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
