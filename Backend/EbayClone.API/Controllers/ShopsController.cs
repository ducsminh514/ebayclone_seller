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
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            try
            {
                var shopId = await _createShopUseCase.ExecuteAsync(userId, request);
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
