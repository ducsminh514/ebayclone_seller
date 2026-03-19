using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Infrastructure.Data;
using EbayClone.Domain.Entities;
using EbayClone.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EbayClone.API.Controllers
{
    // Named record dùng chung cho IMemoryCache — tránh anonymous type mismatch
    internal sealed record CachedCategory(
        Guid Id,
        string Name,
        string Slug,
        Guid? ParentId,
        string? AttributeHints
    );

    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly EbayDbContext _context;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CategoriesController> _logger;

        // Cache keys
        private const string ALL_CATEGORIES_KEY = "categories:all:v1";
        private static readonly TimeSpan CATEGORY_CACHE_TTL = TimeSpan.FromHours(1);

        public CategoriesController(
            EbayDbContext context,
            ICategoryRepository categoryRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<CategoriesController> logger)
        {
            _context = context;
            _categoryRepository = categoryRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        // ─── GET /api/categories ─── (parentId filter + IMemoryCache + N+1 fix)
        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] Guid? parentId = null)
        {
            try
            {
                // ─── CACHE: Load toàn bộ categories 1 lần, cache 1h ───
                // Dùng named record CachedCategory — tránh anonymous type mismatch khi shared cache key
                var allCategories = await _cache.GetOrCreateAsync<List<CachedCategory>>(ALL_CATEGORIES_KEY, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CATEGORY_CACHE_TTL;
                    entry.Priority = CacheItemPriority.High;

                    return await _context.Categories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => new CachedCategory(c.Id, c.Name, c.Slug, c.ParentId, c.AttributeHints))
                        .ToListAsync();
                });

                // ─── N+1 FIX: precompute parentIds set ───
                // Thay vì _context.Categories.Any(...) per-row → N queries
                // Dùng HashSet đã build từ allCategories → O(1) lookup
                var parentIds = allCategories!
                    .Where(c => c.ParentId.HasValue)
                    .Select(c => c.ParentId!.Value)
                    .ToHashSet();

                // ─── FILTER theo parentId ───
                IEnumerable<dynamic> filtered;

                if (parentId.HasValue)
                {
                    filtered = allCategories!.Where(c => c.ParentId == parentId.Value);
                }
                else if (Request.Query.ContainsKey("parentId"))
                {
                    // ?parentId= (empty) → root categories only
                    filtered = allCategories!.Where(c => c.ParentId == null);
                }
                else
                {
                    // No param → tất cả (backward compat)
                    filtered = allCategories!;
                }

                var result = filtered.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.ParentId,
                    c.AttributeHints,
                    HasChildren = parentIds.Contains(c.Id) // O(1) — không hit DB
                }).ToList();

                // ─── HTTP Cache headers: browser cache 1h ───
                Response.Headers.CacheControl = "public, max-age=3600";

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // ─── GET /api/categories/{id}/item-specifics ───
        [HttpGet("{id:guid}/item-specifics")]
        public async Task<IActionResult> GetItemSpecificsByCategory(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"category:specifics:{id}";
                var specifics = await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CATEGORY_CACHE_TTL;
                    return await _categoryRepository.GetItemSpecificsByCategoryIdAsync(id, cancellationToken);
                });

                Response.Headers.CacheControl = "public, max-age=3600";

                return Ok(specifics!.Select(s => new
                {
                    s.Id,
                    s.CategoryId,
                    s.Name,
                    s.Requirement,
                    s.SuggestedValues,
                    s.SortOrder
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading item specifics for category {Id}", id);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // ─── POST /api/categories/suggest-ai ─── (AI suggest, validate vs DB)
        [HttpPost("suggest-ai")]
        public async Task<IActionResult> SuggestByAI([FromBody] SuggestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Keyword) || request.Keyword.Length < 2)
                return Ok(Array.Empty<object>());

            var apiKey = _configuration["GeminiAI:ApiKey"];
            var model = _configuration["GeminiAI:Model"] ?? "gemini-2.5-flash";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
                return StatusCode(503, new { Error = "Gemini API key chưa được cấu hình trong GeminiAI:ApiKey" });

            try
            {
                // Dùng cache đã có — cùng key + cùng type CachedCategory
                var allCategories = await _cache.GetOrCreateAsync<List<CachedCategory>>(ALL_CATEGORIES_KEY, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = CATEGORY_CACHE_TTL;
                    return await _context.Categories
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => new CachedCategory(c.Id, c.Name, c.Slug, c.ParentId, c.AttributeHints))
                        .ToListAsync();
                });

                // Build parentIds set — dùng cho IsLeaf check O(1)
                var parentIdsSet = new HashSet<Guid>(
                    allCategories!.Where(c => c.ParentId.HasValue).Select(c => c.ParentId!.Value));

                // Build full path + IsLeaf cho mọi category
                var categoryList = allCategories!.Select(c =>
                {
                    var parent = allCategories.FirstOrDefault(p => p.Id == c.ParentId);
                    var fullPath = parent != null ? $"{parent.Name} > {c.Name}" : c.Name;
                    var isLeaf = !parentIdsSet.Contains(c.Id);
                    return new { c.Id, c.Name, FullPath = fullPath, IsLeaf = isLeaf };
                }).ToList();

                // Chỉ gửi LEAF categories vào prompt — giảm noise, AI focus hơn
                // Leaf = subcategory cụ thể, root category thường quá chung chung
                var leafCategories = categoryList.Where(c => c.IsLeaf).ToList();
                var promptCategories = leafCategories.Any() ? leafCategories : categoryList;

                var categoryLines = string.Join("\n",
                    promptCategories.Select(c => $"ID={c.Id} | {c.FullPath}"));

                var prompt = $@"You are an eBay category classifier.

Item being sold: ""{request.Keyword}""

Available leaf categories (choose from ONLY these):
{categoryLines}

Instructions:
- Suggest 1-3 most relevant categories from the list above
- Match the keyword to appropriate product categories
- Vietnamese words: op dien thoai=phone case, giay=shoes, quan ao=clothing, etc.
- Output ONLY a JSON array of category IDs (GUIDs), nothing else
- Example output: [""aaaaaaaa-0000-0000-0000-000000000001"",""bbbbbbbb-0000-0000-0000-000000000002""]";

                var httpClient = _httpClientFactory.CreateClient();
                var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { temperature = 0.1, maxOutputTokens = 1000 }
                };

                var httpResponse = await httpClient.PostAsync(geminiUrl,
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // SECURITY: không expose raw response (có thể chứa API key trong URL trace)
                    _logger.LogWarning("Gemini API returned {StatusCode}", httpResponse.StatusCode);
                    return Ok(Array.Empty<object>());
                }

                var rawJson = await httpResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(rawJson);
                var aiText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "[]";

                // DEBUG LOG: xem AI trả gì — xóa sau khi ổn định
                _logger.LogInformation("Gemini raw response for '{Keyword}': {AiText}", request.Keyword, aiText);

                // VALIDATE: chỉ giữ IDs thực sự có trong DB
                var allCategoryIds = new HashSet<Guid>(allCategories!.Select(c => c.Id));

                var jsonStart = aiText.IndexOf('[');
                var jsonEnd = aiText.LastIndexOf(']');
                if (jsonStart < 0 || jsonEnd < 0) return Ok(Array.Empty<object>());

                var jsonArray = aiText[jsonStart..(jsonEnd + 1)];
                var suggestedIds = JsonSerializer.Deserialize<List<string>>(jsonArray) ?? new();

                var validIds = suggestedIds
                    .Where(idStr => Guid.TryParse(idStr, out var g) && allCategoryIds.Contains(g))
                    .Select(Guid.Parse)
                    .Take(3)
                    .ToList();

                if (!validIds.Any())
                {
                    _logger.LogWarning("AI suggested IDs not found in DB for '{Keyword}'. Raw: {AiText}", request.Keyword, aiText);
                    return Ok(Array.Empty<object>());
                }

                var result = validIds
                    .Select(id => categoryList.FirstOrDefault(c => c.Id == id))
                    .Where(c => c != null)
                    .Select(c => new { c!.Id, c.Name, c.FullPath, c.IsLeaf });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini AI for category suggestion");
                return Ok(Array.Empty<object>()); // Graceful fallback
            }
        }

        // ─── GET /api/categories/suggest ─── (Fallback LIKE search)
        [HttpGet("suggest")]
        public async Task<IActionResult> SuggestByKeyword([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
                return Ok(Array.Empty<object>());

            var allCategories = await _cache.GetOrCreateAsync(ALL_CATEGORIES_KEY, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CATEGORY_CACHE_TTL;
                return await _context.Categories
                    .Where(c => c.IsActive)
                    .Select(c => new { c.Id, c.Name, c.Slug, c.ParentId, c.AttributeHints })
                    .ToListAsync();
            });

            var kw = keyword.ToLower();
            var matched = allCategories!
                .Where(c => c.Name.ToLower().Contains(kw)
                         || (c.AttributeHints != null && c.AttributeHints.ToLower().Contains(kw))
                         || c.Slug.ToLower().Contains(kw))
                .Select(c =>
                {
                    var parent = allCategories.FirstOrDefault(p => p.Id == c.ParentId);
                    return new { c.Id, c.Name, FullPath = parent != null ? $"{parent.Name} > {c.Name}" : c.Name };
                })
                .Take(5);

            return Ok(matched);
        }

        // ─── Admin endpoints ───

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
        {
            try
            {
                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Slug = request.Slug,
                    ParentId = request.ParentId,
                    IsActive = true
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                // Invalidate cache sau khi thay đổi
                _cache.Remove(ALL_CATEGORIES_KEY);

                return CreatedAtAction(nameof(GetCategories), new { id = category.Id },
                    new { category.Id, category.Name, category.Slug, category.ParentId });
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null) return NotFound("Không tìm thấy danh mục");

                category.Name = request.Name;
                category.Slug = request.Slug;
                if (request.AttributeHints != null) category.AttributeHints = request.AttributeHints;

                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove(ALL_CATEGORIES_KEY);

                return Ok(new { category.Id, category.Name, category.Slug, category.ParentId, category.AttributeHints });
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null) return NotFound("Không tìm thấy danh mục");

                category.IsActive = false;
                await _context.SaveChangesAsync();

                // Invalidate cache
                _cache.Remove(ALL_CATEGORIES_KEY);

                return NoContent();
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }
    }

    public class SuggestRequest { public string Keyword { get; set; } = string.Empty; }
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? AttributeHints { get; set; }
        public Guid? ParentId { get; set; }
    }
    public class UpdateCategoryDto : CreateCategoryDto { }
}
