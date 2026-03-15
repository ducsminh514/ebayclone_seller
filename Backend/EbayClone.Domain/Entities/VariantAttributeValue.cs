using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// [A1] Lưu attributes của variant dạng relational (thay vì JSON)
    /// để hỗ trợ query/filter/index.
    /// VD: VariantId=X, AttributeName="Size", AttributeValue="S"
    /// </summary>
    public class VariantAttributeValue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid VariantId { get; set; }

        public string AttributeName { get; set; } = string.Empty;   // "Size", "Color"
        public string AttributeValue { get; set; } = string.Empty;  // "S", "Red"

        // Navigation
        public ProductVariant? Variant { get; set; }
    }
}
