using System;
using System.Collections.Generic;

namespace EbayClone.Shared.DTOs.Feedbacks
{
    public class FeedbackDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid ShopId { get; set; }

        public string Rating { get; set; } = string.Empty;
        public string? Comment { get; set; }

        public string? BuyerName { get; set; }    // masked: "d***e"
        public string? OrderNumber { get; set; }
        public decimal OrderAmount { get; set; }

        public string? SellerReply { get; set; }
        public DateTimeOffset? SellerRepliedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }

    public class LeaveFeedbackRequest
    {
        public Guid OrderId { get; set; }
        public string Rating { get; set; } = string.Empty;   // POSITIVE / NEUTRAL / NEGATIVE
        public string? Comment { get; set; }
    }

    public class ReplyFeedbackRequest
    {
        public string Reply { get; set; } = string.Empty;
    }

    public class FeedbackStatsDto
    {
        public int FeedbackScore { get; set; }
        public int TotalPositive { get; set; }
        public int TotalNeutral { get; set; }
        public int TotalNegative { get; set; }
        public decimal PositivePercent { get; set; }
    }
}
