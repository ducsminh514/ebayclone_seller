using EbayClone.Application.UseCases.Feedbacks;
using EbayClone.Shared.DTOs.Feedbacks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.API.Controllers
{
    /// <summary>
    /// Seller-facing feedback management endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly IGetFeedbacksByShopUseCase _getFeedbacksByShopUseCase;
        private readonly IReplyFeedbackUseCase _replyFeedbackUseCase;
        private readonly IGetFeedbackByOrderUseCase _getFeedbackByOrderUseCase;

        public FeedbackController(
            IGetFeedbacksByShopUseCase getFeedbacksByShopUseCase,
            IReplyFeedbackUseCase replyFeedbackUseCase,
            IGetFeedbackByOrderUseCase getFeedbackByOrderUseCase)
        {
            _getFeedbacksByShopUseCase = getFeedbacksByShopUseCase;
            _replyFeedbackUseCase = replyFeedbackUseCase;
            _getFeedbackByOrderUseCase = getFeedbackByOrderUseCase;
        }

        private Guid GetShopId() => Guid.Parse(User.FindFirst("ShopId")!.Value);

        /// <summary>
        /// Seller xem danh sách feedback nhận được (paged).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeedbacks(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? rating = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var shopId = GetShopId();
                var result = await _getFeedbacksByShopUseCase.ExecuteAsync(shopId, page, pageSize, rating, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Seller reply feedback (1 lần duy nhất).
        /// </summary>
        [HttpPost("{feedbackId}/reply")]
        public async Task<IActionResult> ReplyFeedback(
            Guid feedbackId,
            [FromBody] ReplyFeedbackRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var shopId = GetShopId();
                var result = await _replyFeedbackUseCase.ExecuteAsync(shopId, feedbackId, request, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Xem feedback của 1 order cụ thể.
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetFeedbackByOrder(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _getFeedbackByOrderUseCase.ExecuteAsync(orderId, cancellationToken);
                if (result == null) return NotFound(new { Error = "Chưa có feedback cho đơn hàng này." });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy feedback stats cho shop hiện tại.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(
            [FromServices] EbayClone.Application.Interfaces.Repositories.IShopRepository shopRepo,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var shopId = GetShopId();
                var shop = await shopRepo.GetByIdAsync(shopId, cancellationToken);
                if (shop == null) return NotFound();

                return Ok(new FeedbackStatsDto
                {
                    FeedbackScore = shop.FeedbackScore,
                    TotalPositive = shop.TotalPositive,
                    TotalNeutral = shop.TotalNeutral,
                    TotalNegative = shop.TotalNegative,
                    PositivePercent = shop.PositivePercent
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
