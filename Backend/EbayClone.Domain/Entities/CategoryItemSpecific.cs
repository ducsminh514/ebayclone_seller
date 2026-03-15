using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// [A5] Định nghĩa thuộc tính (Item Specific) cho mỗi Category.
    /// Mỗi category có bộ item specifics khác nhau.
    /// VD: Category "Electronics" → Required: "Brand", "Model" | Recommended: "Color"
    /// </summary>
    public class CategoryItemSpecific
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CategoryId { get; set; }

        public string Name { get; set; } = string.Empty;         // "Brand", "Model", "Color"
        public string Requirement { get; set; } = "RECOMMENDED"; // "REQUIRED", "RECOMMENDED", "OPTIONAL"

        // Gợi ý giá trị cho dropdown (JSON array)
        // VD: ["Apple","Samsung","Sony","LG"]
        public string? SuggestedValues { get; set; }

        public int SortOrder { get; set; } = 0;

        // Navigation
        public Category? Category { get; set; }
    }
}
