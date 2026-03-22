using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.UseCases.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EbayClone.API.Controllers
{
    /// <summary>
    /// Test-only controller. CHỈ HOẠT ĐỘNG Ở DEVELOPMENT.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestBuyerController : ControllerBase
    {
        private readonly ICreateTestOrderUseCase _createTestOrderUseCase;
        private readonly EbayClone.Infrastructure.Data.EbayDbContext _db;
        private readonly IWebHostEnvironment _env;

        public TestBuyerController(
            ICreateTestOrderUseCase createTestOrderUseCase,
            EbayClone.Infrastructure.Data.EbayDbContext db,
            IWebHostEnvironment env)
        {
            _createTestOrderUseCase = createTestOrderUseCase;
            _db = db;
            _env = env;
        }

        // Endpoint giả lập khách bấm thanh toán (Không cần Auth để dễ test)
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateBuyerTestOrderRequest request)
        {
            // [Security] Chỉ cho phép ở Development
            if (!_env.IsDevelopment())
            {
                return NotFound(); // Ẩn endpoint ở production
            }

            try
            {
                // Lấy một User bất kỳ từ DB làm người mua giả lập để tránh lỗi Foreign Key
                var mockBuyer = _db.Users.FirstOrDefault(u => u.Role == "BUYER") 
                             ?? _db.Users.FirstOrDefault();

                if (mockBuyer == null)
                {
                    // Nếu DB chưa có user nào, tạo 1 user ảo
                    mockBuyer = new EbayClone.Domain.Entities.User
                    {
                        Id = Guid.NewGuid(),
                        Username = "testbuyer",
                        Email = "testbuyer@example.com",
                        PasswordHash = "mockhash",
                        FullName = "Test Buyer",
                        Role = "BUYER"
                    };
                    _db.Users.Add(mockBuyer);
                    await _db.SaveChangesAsync();
                }

                var orderId = await _createTestOrderUseCase.ExecuteAsync(mockBuyer.Id, request);
                return Ok(new { Message = "Sản phẩm đã được mua. Hàng trong kho đã được trừ (deducted).", OrderId = orderId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetAllActiveProducts()
        {
            // [Security] Chỉ cho phép ở Development
            if (!_env.IsDevelopment())
            {
                return NotFound();
            }

            // Query trực tiếp DB phục vụ test nhanh
            var products = await _db.Products
                .Include(p => p.Variants)
                .Where(p => p.Status == "ACTIVE")
                .ToListAsync();
            return Ok(products);
        }

        // ==================== GĐ2: BUYER MOCK — CANCEL REQUEST ====================

        /// <summary>
        /// Buyer mock: Yêu cầu hủy đơn (trước khi ship)
        /// </summary>
        [HttpPost("cancel-request")]
        public async Task<IActionResult> BuyerCancelRequest(
            [FromBody] BuyerCancelRequest request,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderRepository orderRepo,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderCancellationRepository cancelRepo,
            [FromServices] EbayClone.Application.Interfaces.IUnitOfWork unitOfWork)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var order = await orderRepo.GetByIdAsync(request.OrderId);
                if (order == null) return NotFound(new { Error = "Order not found." });

                // Validate: chỉ cancel khi chưa ship
                if (order.Status != "PAID" && order.Status != "PENDING_PAYMENT")
                    return BadRequest(new { Error = $"Không thể cancel request khi order đang ở trạng thái {order.Status}." });

                // Check: đã có pending request chưa
                var existing = await cancelRepo.GetByOrderIdAsync(request.OrderId);
                if (existing != null && existing.Status == "REQUESTED")
                    return BadRequest(new { Error = "Đã có cancel request đang chờ seller phản hồi." });

                var cancellation = new EbayClone.Domain.Entities.OrderCancellation
                {
                    OrderId = request.OrderId,
                    RequestedBy = "BUYER",
                    Reason = request.Reason ?? "BUYER_ASKED",
                    Notes = "Buyer yêu cầu hủy đơn"
                };
                cancellation.Initialize(); // Auto-set deadline + defect/fee

                await cancelRepo.AddAsync(cancellation);
                await unitOfWork.SaveChangesAsync();

                return Ok(new { Message = "Cancel request đã gửi. Seller có 3 ngày để phản hồi.", CancellationId = cancellation.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ==================== GĐ5A: BUYER MOCK — RETURN FLOW ====================

        /// <summary>
        /// Buyer mock: Open return request (sau DELIVERED)
        /// </summary>
        [HttpPost("return")]
        public async Task<IActionResult> OpenReturn(
            [FromBody] EbayClone.Shared.DTOs.Orders.OpenReturnRequest request,
            [FromServices] EbayClone.Application.UseCases.Orders.IOpenReturnUseCase openReturnUseCase)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var returnId = await openReturnUseCase.ExecuteAsync(request);
                return Ok(new { Message = "Return request đã được tạo. Seller có 3 ngày để phản hồi.", ReturnId = returnId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Buyer mock: Ship hàng return lại (sau seller accept)
        /// </summary>
        [HttpPut("return/{returnId}/ship")]
        public async Task<IActionResult> ShipReturn(
            Guid returnId,
            [FromBody] EbayClone.Shared.DTOs.Orders.ShipReturnRequest request,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderReturnRepository returnRepo,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderRepository orderRepo,
            [FromServices] EbayClone.Application.Interfaces.IUnitOfWork unitOfWork)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var returnEntity = await returnRepo.GetByIdAsync(returnId);
                if (returnEntity == null) return NotFound(new { Error = "Return not found." });

                returnEntity.MarkReturnShipped(request.Carrier, request.TrackingCode);
                returnRepo.Update(returnEntity);

                // Cập nhật Order → RETURN_IN_PROGRESS (nếu chưa)
                if (returnEntity.Order != null && returnEntity.Order.Status == "RETURN_REQUESTED")
                {
                    returnEntity.Order.MarkAsReturnInProgress();
                    orderRepo.Update(returnEntity.Order);
                }

                await unitOfWork.SaveChangesAsync();
                return Ok(new { Message = "Buyer đã gửi hàng return. Seller cần kiểm tra và issue refund." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        // ==================== GĐ5C: BUYER MOCK — DISPUTE FLOW ====================

        /// <summary>
        /// Buyer mock: Open Dispute/Case (INR hoặc SNAD)
        /// </summary>
        [HttpPost("dispute")]
        public async Task<IActionResult> OpenDispute(
            [FromBody] EbayClone.Shared.DTOs.Orders.OpenDisputeRequest request,
            [FromServices] EbayClone.Application.UseCases.Orders.IOpenDisputeUseCase openDisputeUseCase)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var disputeId = await openDisputeUseCase.ExecuteAsync(request);
                return Ok(new { Message = "Dispute đã được mở. Seller có 3 ngày để phản hồi.", DisputeId = disputeId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Buyer mock: Escalate dispute lên platform
        /// </summary>
        [HttpPut("dispute/{disputeId}/escalate")]
        public async Task<IActionResult> EscalateDispute(
            Guid disputeId,
            [FromServices] EbayClone.Application.Interfaces.Repositories.IOrderDisputeRepository disputeRepo,
            [FromServices] EbayClone.Application.Interfaces.IUnitOfWork unitOfWork)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var dispute = await disputeRepo.GetByIdAsync(disputeId);
                if (dispute == null) return NotFound(new { Error = "Dispute not found." });

                dispute.Escalate();
                disputeRepo.Update(dispute);
                await unitOfWork.SaveChangesAsync();
                return Ok(new { Message = "Dispute đã được escalate lên platform để review." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
        // ==================== GĐ6: BUYER MOCK — FEEDBACK ====================

        /// <summary>
        /// Buyer mock: Để lại feedback cho order (sau DELIVERED/COMPLETED)
        /// </summary>
        [HttpPost("feedback")]
        public async Task<IActionResult> LeaveFeedback(
            [FromBody] EbayClone.Shared.DTOs.Feedbacks.LeaveFeedbackRequest request,
            [FromServices] EbayClone.Application.UseCases.Feedbacks.ILeaveFeedbackUseCase leaveFeedbackUseCase)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                // Dùng mock buyer (buyer đầu tiên trong DB)
                var mockBuyer = _db.Users.FirstOrDefault(u => u.Role == "BUYER")
                              ?? _db.Users.FirstOrDefault();
                if (mockBuyer == null)
                    return BadRequest(new { Error = "Không tìm thấy buyer trong DB." });

                var result = await leaveFeedbackUseCase.ExecuteAsync(mockBuyer.Id, request);
                return Ok(new { Message = "Feedback đã được ghi nhận!", Feedback = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Buyer mock: Check đã để feedback cho order chưa
        /// </summary>
        [HttpGet("feedback/{orderId}")]
        public async Task<IActionResult> GetFeedbackByOrder(
            Guid orderId,
            [FromServices] EbayClone.Application.UseCases.Feedbacks.IGetFeedbackByOrderUseCase getFeedbackByOrderUseCase)
        {
            if (!_env.IsDevelopment()) return NotFound();

            try
            {
                var result = await getFeedbackByOrderUseCase.ExecuteAsync(orderId);
                if (result == null) return Ok(new { HasFeedback = false });
                return Ok(new { HasFeedback = true, Feedback = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
