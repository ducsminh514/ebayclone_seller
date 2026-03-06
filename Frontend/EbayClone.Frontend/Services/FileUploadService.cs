using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace EbayClone.Frontend.Services
{
    public class FileUploadService
    {
        private readonly HttpClient _httpClient;

        // Giới hạn kích thước đọc: 10MB (phải khớp với backend)
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        public FileUploadService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Upload 1 ảnh lên server, trả về URL công khai.
        /// </summary>
        public async Task<string> UploadImageAsync(IBrowserFile file)
        {
            if (file.Size > MaxFileSizeBytes)
                throw new Exception($"File '{file.Name}' quá lớn. Tối đa 10MB.");

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(MaxFileSizeBytes);
            using var streamContent = new StreamContent(stream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await _httpClient.PostAsync("api/files/upload-image", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Upload thất bại: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            // Server trả về "Url" (PascalCase) theo C# default serialization
            // Dùng TryGetProperty để không bị crash nếu format thay đổi
            string? url = null;
            if (doc.RootElement.TryGetProperty("Url", out var urlProp) ||
                doc.RootElement.TryGetProperty("url", out urlProp))
            {
                url = urlProp.GetString();
            }

            return url ?? throw new Exception("Server không trả về URL ảnh hợp lệ.");

        }
    }
}
