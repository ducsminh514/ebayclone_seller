using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EbayClone.Application.UseCases.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EbayClone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SELLER")]
    public class DashboardController : ControllerBase
    {
        private readonly IGetDashboardStatsUseCase _getDashboardStatsUseCase;

        public DashboardController(IGetDashboardStatsUseCase getDashboardStatsUseCase)
        {
            _getDashboardStatsUseCase = getDashboardStatsUseCase;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // Bảo mật: Lấy ID Shop từ Token Claim (đã được ShopGuard/Middleware đảm bảo)
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "Bạn chưa đăng ký Shop hoặc không có quyền truy cập dashboard này." });
            }

            try
            {
                var stats = await _getDashboardStatsUseCase.ExecuteAsync(shopId);
                return Ok(stats);
            }
            catch (Exception)
            {
                // [Security] Không leak internal error info
                return StatusCode(500, new { Error = "Đã xảy ra lỗi khi tải dashboard. Vui lòng thử lại sau." });
            }
        }
    }
}
