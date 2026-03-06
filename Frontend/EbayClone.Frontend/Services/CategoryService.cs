using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace EbayClone.Frontend.Services
{
    public class CategoryService
    {
        private readonly HttpClient _httpClient;

        public CategoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                var categories = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>();
                return categories ?? Array.Empty<CategoryDto>();
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Không thể tải danh mục: {error}");
        }
        public async Task<Guid> CreateCategoryAsync(CategoryDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/categories", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi tạo danh mục: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
            return result?.Id ?? Guid.Empty;
        }

        public async Task UpdateCategoryAsync(Guid id, CategoryDto request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi cập nhật: {error}");
            }
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/categories/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi xóa danh mục: {error}");
            }
        }
    }

    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Gợi ý thuộc tính từ server (JSON string: ["RAM","Màu sắc"])
        public string? AttributeHints { get; set; }

        // Đã parse thành List để dùng trực tiếp trong UI
        public List<string> SuggestedAttributes =>
            !string.IsNullOrEmpty(AttributeHints)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(AttributeHints) ?? new()
                : new();
    }
}
