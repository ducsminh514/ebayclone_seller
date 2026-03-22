using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.UseCases.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ITrackProductViewUseCase _trackViewUseCase;
        private readonly IGetTrafficStatsUseCase _getTrafficUseCase;

        public AnalyticsController(
            ITrackProductViewUseCase trackViewUseCase,
            IGetTrafficStatsUseCase getTrafficUseCase)
        {
            _trackViewUseCase = trackViewUseCase;
            _getTrafficUseCase = getTrafficUseCase;
        }

        /// <summary>
        /// Track product view (mock buyer visiting listing page).
        /// Rate limited: 1 view per IP per product per hour.
        /// </summary>
        [HttpPost("products/{productId}/view")]
        public async Task<IActionResult> TrackView(Guid productId, CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var recorded = await _trackViewUseCase.ExecuteAsync(productId, ip, ct);

            return recorded
                ? Ok(new { recorded = true, message = "View recorded" })
                : Ok(new { recorded = false, message = "Rate limited or product not active" });
        }

        /// <summary>
        /// Get traffic stats for seller dashboard.
        /// </summary>
        [HttpGet("traffic/{shopId}")]
        public async Task<IActionResult> GetTrafficStats(Guid shopId, [FromQuery] int days = 30, CancellationToken ct = default)
        {
            if (days < 1 || days > 90) days = 30;

            var stats = await _getTrafficUseCase.ExecuteAsync(shopId, days, ct);
            return Ok(stats);
        }
    }
}
