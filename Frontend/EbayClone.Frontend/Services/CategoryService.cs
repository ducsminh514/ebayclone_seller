using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Categories;

namespace EbayClone.Frontend.Services
{
    /// <summary>
    /// Scoped service — an toàn với HttpClient (cũng Scoped).
    /// Dùng CategoryCacheService (Singleton) như tầng 1 cache:
    ///   1. Check Singleton cache → hit: trả ngay (no API call)
    ///   2. Miss: gọi API → lưu vào Singleton cache → trả về
    /// </summary>
    public class CategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly CategoryCacheService _localCache; // Singleton cache, tồn tại suốt session

        public CategoryService(HttpClient httpClient, CategoryCacheService localCache)
        {
            _httpClient = httpClient;
            _localCache = localCache;
        }

        /// <summary>Lấy tất cả categories (backward compat — cho dropdown fallback)</summary>
        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            // Dùng AllCategories cache nếu có
            if (_localCache.AllCategories != null)
                return _localCache.AllCategories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ParentId = c.ParentId
                });

            var response = await _httpClient.GetAsync("api/categories");
            if (response.IsSuccessStatusCode)
            {
                var categories = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>();
                return categories ?? Array.Empty<CategoryDto>();
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Không thể tải danh mục: {error}");
        }

        /// <summary>Lấy root categories — check Singleton cache trước</summary>
        public async Task<IEnumerable<CategoryTreeNodeDto>> GetRootCategoriesAsync()
        {
            // Cache hit
            if (_localCache.RootCategories != null)
                return _localCache.RootCategories;

            var response = await _httpClient.GetAsync("api/categories?parentId=");
            if (response.IsSuccessStatusCode)
            {
                var cats = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryTreeNodeDto>>();
                var list = (cats ?? Array.Empty<CategoryTreeNodeDto>()).ToList();

                // Lưu vào Singleton cache
                _localCache.RootCategories = list;
                if (_localCache.AllCategories == null) _localCache.AllCategories = new(list);
                else foreach (var c in list) if (!_localCache.AllCategories.Any(a => a.Id == c.Id)) _localCache.AllCategories.Add(c);

                return list;
            }
            return Array.Empty<CategoryTreeNodeDto>();
        }

        /// <summary>Lấy children của một node — check cache theo parentId trước</summary>
        public async Task<IEnumerable<CategoryTreeNodeDto>> GetChildCategoriesAsync(Guid parentId)
        {
            // Cache hit
            if (_localCache.ChildrenCache.TryGetValue(parentId, out var cached))
                return cached;

            var response = await _httpClient.GetAsync($"api/categories?parentId={parentId}");
            if (response.IsSuccessStatusCode)
            {
                var cats = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryTreeNodeDto>>();
                var list = (cats ?? Array.Empty<CategoryTreeNodeDto>()).ToList();

                // Lưu vào cache
                _localCache.ChildrenCache[parentId] = list;
                if (_localCache.AllCategories == null) _localCache.AllCategories = new(list);
                else foreach (var c in list) if (!_localCache.AllCategories.Any(a => a.Id == c.Id)) _localCache.AllCategories.Add(c);

                return list;
            }
            return Array.Empty<CategoryTreeNodeDto>();
        }

        /// <summary>AI suggest categories từ keyword — không cache (dynamic per keyword)</summary>
        public async Task<IEnumerable<AiSuggestResultDto>> SuggestCategoriesAiAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return Array.Empty<AiSuggestResultDto>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/categories/suggest-ai", new { keyword });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<IEnumerable<AiSuggestResultDto>>();
                    return result ?? Array.Empty<AiSuggestResultDto>();
                }
            }
            catch { /* silent fallback — BE trả 503 nếu chưa có API key */ }

            return Array.Empty<AiSuggestResultDto>();
        }

        public async Task<Guid> CreateCategoryAsync(CategoryDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/categories", request);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Lỗi tạo danh mục: {await response.Content.ReadAsStringAsync()}");

            _localCache.Invalidate(); // Clear cache sau khi thêm mới
            var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
            return result?.Id ?? Guid.Empty;
        }

        public async Task UpdateCategoryAsync(Guid id, CategoryDto request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", request);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Lỗi cập nhật: {await response.Content.ReadAsStringAsync()}");
            _localCache.Invalidate(); // Clear cache
        }

        public async Task DeleteCategoryAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/categories/{id}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Lỗi xóa danh mục: {await response.Content.ReadAsStringAsync()}");
            _localCache.Invalidate(); // Clear cache
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
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Array.Empty<CategoryItemSpecificDto>();

            throw new Exception($"Không thể tải Item Specifics: {await response.Content.ReadAsStringAsync()}");
        }
    }

    // ─── DTOs ───

    public class CategoryItemSpecificDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Requirement { get; set; } = "OPTIONAL";
        public string? SuggestedValues { get; set; }
        public int SortOrder { get; set; }
    }

    public class CategoryTreeNodeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string? AttributeHints { get; set; }
        public bool HasChildren { get; set; }
    }

    public class AiSuggestResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsLeaf { get; set; }
    }
}
