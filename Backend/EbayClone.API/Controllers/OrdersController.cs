using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.UseCases.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SELLER")]
    public class OrdersController : ControllerBase
    {
        private readonly IGetOrdersUseCase _getOrdersUseCase;
        private readonly IGetOrderByIdUseCase _getOrderByIdUseCase;
        private readonly IUpdateOrderStatusUseCase _updateOrderStatusUseCase;

        public OrdersController(
            IGetOrdersUseCase getOrdersUseCase,
            IGetOrderByIdUseCase getOrderByIdUseCase,
            IUpdateOrderStatusUseCase updateOrderStatusUseCase)
        {
            _getOrdersUseCase = getOrdersUseCase;
            _getOrderByIdUseCase = getOrderByIdUseCase;
            _updateOrderStatusUseCase = updateOrderStatusUseCase;
        }

        private Guid? GetShopId()
        {
            var shopIdClaim = User.Claims.FirstOrDefault(c => c.Type == "ShopId")?.Value;
            if (Guid.TryParse(shopIdClaim, out var shopId))
            {
                return shopId;
            }
            return null;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetShopOrders(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? status = null, 
            [FromQuery] string? searchQuery = null)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue)
            {
                return StatusCode(403, new { Error = "Account not authorized. HasShop claim missing. Please relogin to refresh claims." });
            }

            // [Performance] Cap pageSize để tránh DoS
            pageSize = Math.Clamp(pageSize, 1, 50);
            pageNumber = Math.Max(1, pageNumber);

            var result = await _getOrdersUseCase.ExecutePagedAsync(
                shopId.Value, pageNumber, pageSize, status, searchQuery);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue)
            {
                return StatusCode(403, new { Error = "Account not authorized." });
            }

            var order = await _getOrderByIdUseCase.ExecuteAsync(shopId.Value, id);
            if (order == null) return NotFound(new { Error = "Order not found or does not belong to your shop." });
            
            return Ok(order);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            // [Validation] Chặn body null hoặc NewStatus rỗng
            if (request == null || string.IsNullOrWhiteSpace(request.NewStatus))
            {
                return BadRequest(new { Error = "Request body hoặc NewStatus không được để trống." });
            }

            var shopId = GetShopId();
            if (!shopId.HasValue)
            {
                return StatusCode(403, new { Error = "Account not authorized." });
            }

            try
            {
                await _updateOrderStatusUseCase.ExecuteAsync(shopId.Value, id, request);
                return Ok(new { Message = "Order status updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message }); // Lỗi State Machine (Nhảy cóc Status)
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { Error = ex.Message }); // Cố tình đổi Order của Shop khác
            }
        }

        // ==================== GĐ2: CANCEL REQUEST ENDPOINTS ====================

        /// <summary>
        /// GET pending cancel request cho 1 order (nếu có).
        /// </summary>
        [HttpGet("{orderId}/cancel-request")]
        public async Task<IActionResult> GetCancelRequest(
            Guid orderId,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderCancellationRepository cancelRepo,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderRepository orderRepo)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue)
                return StatusCode(403, new { Error = "Account not authorized." });

            var order = await orderRepo.GetByIdAsync(orderId);
            if (order == null || order.ShopId != shopId.Value)
                return NotFound(new { Error = "Order not found." });

            var cancellation = await cancelRepo.GetByOrderIdAsync(orderId);
            if (cancellation == null || cancellation.Status != "REQUESTED")
                return Ok(new { HasPendingRequest = false });

            return Ok(new
            {
                HasPendingRequest = true,
                CancellationId = cancellation.Id,
                Reason = cancellation.Reason,
                RequestedBy = cancellation.RequestedBy,
                Notes = cancellation.Notes,
                RequestedAt = cancellation.RequestedAt,
                ResponseDeadline = cancellation.ResponseDeadline
            });
        }

        // ==================== GĐ2: SELLER RESPOND CANCEL REQUEST ====================

        /// <summary>
        /// Seller accept/decline buyer cancel request.
        /// Accept → cancel order (refund + restock). Decline → buyer chờ hàng rồi Return.
        /// </summary>
        [HttpPut("{orderId}/cancel-request/respond")]
        public async Task<IActionResult> RespondCancelRequest(
            Guid orderId,
            [FromBody] RespondCancelRequest request,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderCancellationRepository cancelRepo,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderRepository orderRepo,
            [FromServices] EbayClone.Application.Interfaces.IUnitOfWork unitOfWork)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue)
                return StatusCode(403, new { Error = "Account not authorized." });

            try
            {
                // Lấy pending cancel request (KHÔNG lấy order riêng — để UseCase lấy, tránh DbContext tracking conflict)
                var cancellation = await cancelRepo.GetByOrderIdAsync(orderId);
                if (cancellation == null || cancellation.Status != "REQUESTED")
                    return BadRequest(new { Error = "Không có cancel request nào đang chờ phản hồi." });

                if (request.Accept)
                {
                    // Accept cancellation record trước
                    cancellation.Accept();
                    cancellation.MarkCompleted();
                    cancelRepo.Update(cancellation);
                    await unitOfWork.SaveChangesAsync(); // Save cancellation TRƯỚC

                    // [BUG-2 FIX] Dùng UpdateOrderStatusUseCase để cancel 
                    // UseCase sẽ tự fetch order + xử lý transaction riêng → không conflict DbContext
                    // Lấy RowVersion mới nhất từ order (không dùng biến order cũ)
                    var freshOrder = await orderRepo.GetByIdAsync(orderId);
                    if (freshOrder == null || freshOrder.ShopId != shopId.Value)
                        return NotFound(new { Error = "Order not found." });

                    var cancelRequest = new UpdateOrderStatusRequest
                    {
                        NewStatus = "CANCELLED",
                        RowVersion = freshOrder.RowVersion,
                        CancelReason = cancellation.Reason,
                        CancelRequestedBy = "BUYER",
                        CancelNotes = "Seller accepted buyer cancel request"
                    };
                    await _updateOrderStatusUseCase.ExecuteAsync(shopId.Value, orderId, cancelRequest);

                    return Ok(new { Message = "Đã chấp nhận yêu cầu hủy. Đơn hàng đã được hủy + hoàn tiền." });
                }
                else
                {
                    // Decline → chỉ cập nhật cancellation record
                    cancellation.Decline(request.DeclineNotes);
                    cancelRepo.Update(cancellation);
                    await unitOfWork.SaveChangesAsync();

                    return Ok(new { Message = "Đã từ chối yêu cầu hủy. Buyer sẽ nhận hàng và có thể mở Return." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Release escrowed funds. BẢO MẬT: Chỉ ADMIN hoặc background job (API key) mới được trigger.
        /// </summary>
        [AllowAnonymous] // Override class-level Authorize — dùng API key check bên dưới
        [HttpPost("release-funds")]
        public async Task<IActionResult> ReleaseFunds(
            [FromServices] IReleaseFundsUseCase releaseFundsUseCase,
            [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey)
        {
            // [Security] Check: phải là ADMIN role HOẶC internal API key
            var isAdmin = User.IsInRole("ADMIN");
            var isValidApiKey = !string.IsNullOrEmpty(apiKey) && apiKey == "ebay-internal-fund-release-key-2024";
            
            if (!isAdmin && !isValidApiKey)
            {
                return StatusCode(403, new { Error = "Chỉ Admin hoặc hệ thống nội bộ mới có quyền release funds." });
            }

            try
            {
                var count = await releaseFundsUseCase.ExecuteAsync();
                return Ok(new { ReleasedOrdersCount = count, Message = $"Successfully released funds for {count} orders." });
            }
            catch (Exception)
            {
                // [Security] Không trả ex.Message → tránh leak thông tin nội bộ
                return StatusCode(500, new { Error = "Đã xảy ra lỗi trong quá trình release funds. Vui lòng thử lại sau." });
            }
        }

        // ==================== GĐ5A: RETURN/REFUND FLOW ====================

        /// <summary>
        /// Seller respond to return request (4 options: ACCEPT_RETURN, PARTIAL_REFUND, FULL_REFUND_KEEP_ITEM, DECLINE)
        /// </summary>
        [HttpPut("returns/{returnId}/respond")]
        public async Task<IActionResult> RespondReturn(
            Guid returnId, 
            [FromBody] RespondReturnRequest request,
            [FromServices] IRespondReturnUseCase respondReturnUseCase)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue) return StatusCode(403, new { Error = "Account not authorized." });

            try
            {
                await respondReturnUseCase.ExecuteAsync(shopId.Value, returnId, request);
                return Ok(new { Message = $"Return request đã được xử lý ({request.ResponseType})." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
        }

        /// <summary>
        /// Seller issue refund sau khi nhận hàng return (inspect + quyết định stock)
        /// </summary>
        [HttpPost("returns/{returnId}/refund")]
        public async Task<IActionResult> IssueRefund(
            Guid returnId,
            [FromBody] IssueRefundRequest request,
            [FromServices] IIssueRefundUseCase issueRefundUseCase)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue) return StatusCode(403, new { Error = "Account not authorized." });

            try
            {
                await issueRefundUseCase.ExecuteAsync(shopId.Value, returnId, request);
                return Ok(new { Message = "Refund đã được xử lý thành công." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
        }

        // ==================== GĐ5C: DISPUTE/CASE FLOW ====================

        /// <summary>
        /// Seller respond to dispute — nộp bằng chứng (tracking, ảnh đóng hàng)
        /// </summary>
        [HttpPut("disputes/{disputeId}/respond")]
        public async Task<IActionResult> RespondDispute(
            Guid disputeId,
            [FromBody] RespondDisputeRequest request,
            [FromServices] IRespondDisputeUseCase respondDisputeUseCase)
        {
            var shopId = GetShopId();
            if (!shopId.HasValue) return StatusCode(403, new { Error = "Account not authorized." });

            try
            {
                await respondDisputeUseCase.ExecuteAsync(shopId.Value, disputeId, request);
                return Ok(new { Message = "Đã nộp phản hồi dispute thành công." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
        }

        /// <summary>
        /// Platform resolve dispute — chỉ ADMIN.
        /// Buyer win → REFUNDED + defect. Seller win → COMPLETED + funds release.
        /// </summary>
        [HttpPost("disputes/{disputeId}/resolve")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> ResolveDispute(
            Guid disputeId,
            [FromBody] ResolveDisputeRequest request,
            [FromServices] IResolveDisputeUseCase resolveDisputeUseCase)
        {
            try
            {
                await resolveDisputeUseCase.ExecuteAsync(disputeId, request);
                return Ok(new { Message = $"Dispute đã được resolve ({request.Resolution})." });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
        }
    }
}
