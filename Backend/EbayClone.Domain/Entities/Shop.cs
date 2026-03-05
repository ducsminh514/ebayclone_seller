using System;

namespace EbayClone.Domain.Entities
{
    public class Shop
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string? BannerUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal RatingAvg { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        public User? Owner { get; set; }
    }
}
