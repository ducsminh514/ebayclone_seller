namespace EbayClone.Frontend.Services
{
    /// <summary>
    /// Resolve image URLs từ relative (/uploads/...) sang absolute (http://localhost/uploads/...)
    /// Cần thiết vì Blazor WASM chạy trên port 7251 (khác port API) → relative URL resolve sai origin.
    /// </summary>
    public class ImageUrlService
    {
        private readonly string _apiBaseUrl;

        public ImageUrlService(string apiBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        }

        /// <summary>
        /// Nếu URL là relative (/uploads/...) → prepend ApiBaseUrl.
        /// Nếu URL đã là absolute (http://...) → giữ nguyên.
        /// </summary>
        public string Resolve(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return string.Empty;

            // Đã là absolute URL → giữ nguyên
            if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
                return imageUrl;

            // Relative URL → prepend API base
            return $"{_apiBaseUrl}{imageUrl}";
        }
    }
}
