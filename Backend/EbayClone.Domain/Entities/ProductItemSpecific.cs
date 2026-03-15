using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// [A5] Giá trị Item Specific mà Seller nhập cho sản phẩm cụ thể.
    /// VD: ProductId=X, Name="Brand", Value="Apple"
    /// </summary>
    public class ProductItemSpecific
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }

        public string Name { get; set; } = string.Empty;    // "Brand"
        public string Value { get; set; } = string.Empty;    // "Apple"

        // Navigation
        public Product? Product { get; set; }
    }
}
