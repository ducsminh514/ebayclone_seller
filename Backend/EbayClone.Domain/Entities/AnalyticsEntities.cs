using System;

namespace EbayClone.Domain.Entities
{
    public class ShopAnalyticsDaily
    {
        public Guid ShopId { get; set; }
        public DateTime ReportDate { get; set; }
        
        public decimal TotalRevenue { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public int ItemsSold { get; set; } = 0;
        public int ViewsCount { get; set; } = 0;
        
        // Navigation
        public Shop? Shop { get; set; }
    }

    // Review entity REMOVED — replaced by Feedback entity (Phần 6 CRM)
    // See: EbayClone.Domain.Entities.Feedback

    public class ProductViewLog
    {
        public long Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid ProductId { get; set; }
        public string? ViewerIP { get; set; }
        public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        public Shop? Shop { get; set; }
        public Product? Product { get; set; }
    }
}
