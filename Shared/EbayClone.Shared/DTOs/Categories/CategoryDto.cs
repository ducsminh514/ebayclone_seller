using System;
using System.Collections.Generic;

namespace EbayClone.Shared.DTOs.Categories
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Gợi ý thuộc tính từ server (JSON string: ["RAM","Màu sắc"])
        public string? AttributeHints { get; set; }

        public List<string> SuggestedAttributes =>
            !string.IsNullOrEmpty(AttributeHints)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(AttributeHints) ?? new()
                : new();
    }
}
