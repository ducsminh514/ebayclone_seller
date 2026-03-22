using System;

namespace EbayClone.Domain.Entities
{
    public class Feedback
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relationships
        public Guid OrderId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid ShopId { get; set; }

        // Rating: POSITIVE / NEUTRAL / NEGATIVE
        public string Rating { get; set; } = string.Empty;

        // Buyer comment (max 500 chars)
        public string? Comment { get; set; }

        // Seller reply (1 lần duy nhất)
        public string? SellerReply { get; set; }
        public DateTimeOffset? SellerRepliedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public Order? Order { get; set; }
        public User? Buyer { get; set; }
        public Shop? Shop { get; set; }

        // === Domain Methods ===

        /// <summary>
        /// Seller reply feedback. Chỉ cho phép 1 lần.
        /// </summary>
        public void SetSellerReply(string reply)
        {
            if (SellerReply != null)
                throw new InvalidOperationException("Seller đã reply feedback này rồi. Không thể reply lần 2.");

            if (string.IsNullOrWhiteSpace(reply))
                throw new ArgumentException("Reply không được để trống.");

            if (reply.Length > 1000)
                throw new ArgumentException("Reply không được vượt quá 1000 ký tự.");

            SellerReply = reply.Trim();
            SellerRepliedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Validate feedback trước khi tạo.
        /// </summary>
        public static Feedback Create(Guid orderId, Guid buyerId, Guid shopId, string rating, string? comment)
        {
            // Validate rating
            if (rating != FeedbackRatings.POSITIVE &&
                rating != FeedbackRatings.NEUTRAL &&
                rating != FeedbackRatings.NEGATIVE)
            {
                throw new ArgumentException($"Rating không hợp lệ: {rating}. Chỉ chấp nhận POSITIVE, NEUTRAL, NEGATIVE.");
            }

            // Validate comment
            if (comment != null && comment.Length > 500)
                throw new ArgumentException("Comment không được vượt quá 500 ký tự.");

            return new Feedback
            {
                OrderId = orderId,
                BuyerId = buyerId,
                ShopId = shopId,
                Rating = rating,
                Comment = comment?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public static class FeedbackRatings
    {
        public const string POSITIVE = "POSITIVE";
        public const string NEUTRAL = "NEUTRAL";
        public const string NEGATIVE = "NEGATIVE";
    }
}
