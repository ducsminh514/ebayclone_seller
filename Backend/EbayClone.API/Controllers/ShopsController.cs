using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Shops;
using EbayClone.Application.UseCases.Shops;

namespace EbayClone.API.Controllers
{
    [ApiController]
    public class ShopsController : ControllerBase
    {
        private readonly ICreateShopUseCase _createShopUseCase;

        public ShopsController(ICreateShopUseCase createShopUseCase)
        {
            _createShopUseCase = createShopUseCase;
        }

        /// <summary>
        /// Người bán gửi yêu cầu mở Shop (Tự động duyệt và sinh Ví)
        /// </summary>
        [HttpPost("api/shops/kyc")]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
        {
            // Tạm thời mock UserId đã được Insert cứng vào DB cản FK Error
            var mockUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            try
            {
                var shopId = await _createShopUseCase.ExecuteAsync(mockUserId, request);
                return CreatedAtAction(nameof(GetShopById), new { id = shopId }, new { Id = shopId, Message = "Shop created, auto-approved and Wallet initialized successfully." });
            }
            catch (DbUpdateException ex)
            {
                // Bắt lỗi Unique Index (1 User - 1 Shop) từ Data Engine
                return Conflict(new { Error = "Conflict! User already has a shop or Data is duplicated.", Details = ex.InnerException?.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // Placeholder for created at result
        [HttpGet("api/shops/{id}")]
        public IActionResult GetShopById(Guid id)
        {
            return Ok(new { Id = id }); // Mock return
        }
    }
}
