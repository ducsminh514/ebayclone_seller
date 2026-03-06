using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;

namespace EbayClone.Frontend.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Guid> CreateListingAsync(CreateListingRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/products", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Không thể tạo sản phẩm: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
            return result?.Id ?? Guid.Empty;
        }

        public async Task RestockVariantAsync(Guid variantId, int addedQuantity)
        {
            var request = new RestockVariantRequest { AddedQuantity = addedQuantity };
            var response = await _httpClient.PutAsJsonAsync($"api/products/variants/{variantId}/restock", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi nhập kho: {error}");
            }
        }

        public async Task<EbayClone.Domain.Entities.Product?> GetProductByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"api/products/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EbayClone.Domain.Entities.Product>();
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Không thể tải sản phẩm: {error}");
        }

        public async Task UpdateProductBasicAsync(Guid id, UpdateProductBasicRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi cập nhật: {error}");
            }
        }

        public async Task UpdateProductStatusAsync(Guid id, string status)
        {
            var request = new UpdateProductStatusRequest { Status = status };
            var response = await _httpClient.PatchAsJsonAsync($"api/products/{id}/status", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi cập nhật trạng thái: {error}");
            }
        }

        public async Task<IEnumerable<EbayClone.Domain.Entities.Product>> GetMyProductsAsync()
        {
            var response = await _httpClient.GetAsync("api/products");
            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<IEnumerable<EbayClone.Domain.Entities.Product>>();
                return products ?? Array.Empty<EbayClone.Domain.Entities.Product>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new Exception("Tài khoản của bạn chưa được cấp quyền SELLER. Vui lòng bấm Đăng xuất và Đăng nhập lại để làm mới Token!");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new Exception("Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi máy chủ trả về: {response.StatusCode} - {error}");
            }
        }
    }

    public class CreateProductResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
