using System;

namespace EbayClone.Domain.Entities
{
    public class FileEntity // Named FileEntity to avoid conflict with System.IO.File
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Type { get; set; } // 'PRODUCT_IMAGE', 'AVATAR', 'DOCUMENT'
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        
        // Navigation
        public User? Owner { get; set; }
    }
}
