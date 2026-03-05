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
    
    public class Review
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid BuyerId { get; set; }
        
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public string? SellerReply { get; set; }
        public DateTimeOffset? RepliedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public User? Buyer { get; set; }
    }
    
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
