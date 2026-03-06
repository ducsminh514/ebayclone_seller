using System;
using System.Linq;
using System.Threading.Tasks;
using EbayClone.Infrastructure.Data;
using EbayClone.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly EbayDbContext _context;

        public CategoriesController(EbayDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories.ToListAsync();
                
                // Tự động Seed dữ liệu mẫu nếu bảng Categories rỗng
                if (!categories.Any())
                {
                    categories = new System.Collections.Generic.List<Category>
                    {
                        new Category { Id = Guid.NewGuid(), Name = "Điện Thoại & Phụ Kiện", Slug = "dien-thoai-phu-kien" },
                        new Category { Id = Guid.NewGuid(), Name = "Thời Trang Nam", Slug = "thoi-trang-nam" },
                        new Category { Id = Guid.NewGuid(), Name = "Thời Trang Nữ", Slug = "thoi-trang-nu" },
                        new Category { Id = Guid.NewGuid(), Name = "Nhà Cửa & Đời Sống", Slug = "nha-cua-doi-song" }
                    };
                    
                    await _context.Categories.AddRangeAsync(categories);
                    await _context.SaveChangesAsync();
                }

                return Ok(categories.Where(c => c.IsActive).Select(c => new { c.Id, c.Name, c.Slug }));
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
                    IsActive = true
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, new { category.Id, category.Name, category.Slug });
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

                await _context.SaveChangesAsync();
                return Ok(new { category.Id, category.Name, category.Slug });
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
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
    }
}
