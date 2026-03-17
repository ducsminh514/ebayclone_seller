using System;
using System.Linq;
using System.Threading.Tasks;
using EbayClone.Infrastructure.Data;
using EbayClone.Domain.Entities;
using EbayClone.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly EbayDbContext _context;
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(EbayDbContext context, ICategoryRepository categoryRepository)
        {
            _context = context;
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                // Không inline seed ở đây — CategorySeeder chạy ở app startup
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                return Ok(categories.Select(c => new 
                { 
                    c.Id, 
                    c.Name, 
                    c.Slug, 
                    c.ParentId,           // [A7] Hỗ trợ FE hiển thị category tree
                    c.AttributeHints 
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // [A5] Endpoint mới: Lấy Item Specifics theo Category
        // FE cần gọi khi seller chọn category → hiện form nhập required/recommended fields
        [HttpGet("{id:guid}/item-specifics")]
        public async Task<IActionResult> GetItemSpecificsByCategory(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var specifics = await _categoryRepository.GetItemSpecificsByCategoryIdAsync(id, cancellationToken);
                
                return Ok(specifics.Select(s => new
                {
                    s.Id,
                    s.CategoryId,
                    s.Name,
                    s.Requirement, // "REQUIRED", "RECOMMENDED", "OPTIONAL"
                    s.SuggestedValues,
                    s.SortOrder
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

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

                return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, 
                    new { category.Id, category.Name, category.Slug, category.ParentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
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
                if (request.AttributeHints != null)
                    category.AttributeHints = request.AttributeHints;

                await _context.SaveChangesAsync();
                return Ok(new { category.Id, category.Name, category.Slug, category.ParentId, category.AttributeHints });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null) return NotFound("Không tìm thấy danh mục");

                // Soft delete
                category.IsActive = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? AttributeHints { get; set; }
        public Guid? ParentId { get; set; }
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
    }
}
