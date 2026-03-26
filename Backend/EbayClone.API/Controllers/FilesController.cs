using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("upload_image")]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // Giới hạn kích thước: 10MB
        private const long MaxFileSize = 10 * 1024 * 1024;

        // Extension được phép (lớp bảo vệ đầu tiên)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        // Magic bytes (file header signature) của từng loại ảnh hợp lệ
        // Lớp bảo vệ thứ 2: đọc nội dung thực của file, không phụ thuộc tên file
        // → Chặn kẻ tấn công đổi tên malware.exe → photo.jpg để bypass extension check
        private static readonly Dictionary<string, byte[][]> MagicBytes = new()
        {
            [".jpg"]  = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"]  = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".gif"]  = new[] {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37 }, // GIF87a
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39 }  // GIF89a
            },
            [".webp"] = new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } }, // RIFF header
        };

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Upload ảnh từ máy tính.
        /// Lưu vào wwwroot/uploads/images/ → trả về URL để lưu vào DB.
        /// Bảo mật 2 lớp: extension check + magic bytes check.
        /// </summary>
        [HttpPost("upload-image")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Không có file được gửi lên." });

            if (file.Length > MaxFileSize)
                return BadRequest(new { Error = "File quá lớn. Tối đa 10MB." });

            // Layer 1: Check extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Array.Exists(AllowedExtensions, e => e == ext))
                return BadRequest(new { Error = $"Định dạng không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedExtensions)}" });

            // Layer 2: Check magic bytes (signature thực của file)
            // Kẻ tấn công có thể đổi tên file, nhưng không thể giả nội dung nhị phân
            if (!await IsValidImageMagicBytesAsync(file, ext))
                return BadRequest(new { Error = "Nội dung file không hợp lệ. Upload bị từ chối vì lý do bảo mật." });

            // Tạo thư mục nếu chưa có
            var uploadFolder = Path.Combine(
                _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                "uploads", "images");
            Directory.CreateDirectory(uploadFolder);

            // UUID filename để tránh path traversal và tên file trùng lặp
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Lưu file vào disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về relative URL — lưu vào DB dạng /uploads/images/xxx.jpg
            // Frontend sẽ dùng ImageUrlService.Resolve() để prepend ApiBaseUrl khi render
            // Lý do relative: không hardcode host vào DB → chạy đúng mọi environment
            var publicUrl = $"/uploads/images/{uniqueFileName}";

            return Ok(new { Url = publicUrl, FileName = uniqueFileName });
        }

        /// <summary>
        /// Đọc bytes đầu của file, so sánh với magic signatures đã biết.
        /// </summary>
        private static async Task<bool> IsValidImageMagicBytesAsync(IFormFile file, string ext)
        {
            if (!MagicBytes.TryGetValue(ext, out var signatures))
                return false;

            var header = new byte[8];
            using var stream = file.OpenReadStream();
            var bytesRead = await stream.ReadAsync(header, 0, header.Length);
            if (bytesRead < 3) return false;

            foreach (var sig in signatures)
            {
                if (bytesRead >= sig.Length && HeaderMatchesSig(header, sig))
                    return true;
            }
            return false;
        }

        private static bool HeaderMatchesSig(byte[] header, byte[] sig)
        {
            for (int i = 0; i < sig.Length; i++)
            {
                if (header[i] != sig[i]) return false;
            }
            return true;
        }
    }
}
