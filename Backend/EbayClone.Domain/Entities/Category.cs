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
        public string? AttributeHints { get; set; } // JSON metadata for dynamic attributes
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Category? Parent { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
}
