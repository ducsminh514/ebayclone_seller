using System;
using System.Collections.Generic;

namespace EbayClone.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Gợi ý thuộc tính biến thể cho Seller khi chọn danh mục này
        // Lưu JSON array: ["Ram","Bộ nhớ","Màu sắc"]
        public string? AttributeHints { get; set; }

        // Navigation properties
        public Category? Parent { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
}
