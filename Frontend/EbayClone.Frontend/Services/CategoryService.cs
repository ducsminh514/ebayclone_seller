using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Categories;

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

        // [A5] Lấy Item Specifics theo Category ID
        public async Task<IEnumerable<CategoryItemSpecificDto>> GetItemSpecificsByCategoryIdAsync(Guid categoryId)
        {
            var response = await _httpClient.GetAsync($"api/categories/{categoryId}/item-specifics");
            if (response.IsSuccessStatusCode)
            {
                var specifics = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryItemSpecificDto>>();
                return specifics ?? Array.Empty<CategoryItemSpecificDto>();
            }

            // Nếu category chưa có specifics → trả rỗng (không throw)
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) 
                return Array.Empty<CategoryItemSpecificDto>();
            
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Không thể tải Item Specifics: {error}");
        }
    }

    // DTO cho CategoryItemSpecific response
    public class CategoryItemSpecificDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Requirement { get; set; } = "OPTIONAL"; // REQUIRED, RECOMMENDED, OPTIONAL
        public string? SuggestedValues { get; set; }
        public int SortOrder { get; set; }
    }
}
