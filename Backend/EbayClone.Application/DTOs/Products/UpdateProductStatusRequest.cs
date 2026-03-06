using System;

namespace EbayClone.Application.DTOs.Products
{
    public class UpdateProductStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        // Bắt buộc khi Status = "SCHEDULED" - nếu thiếu backend sẽ từ chối
        public DateTimeOffset? ScheduledAt { get; set; }
    }
}
