using System;

namespace EbayClone.Shared.DTOs.Products
{
    public class CreateProductResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
