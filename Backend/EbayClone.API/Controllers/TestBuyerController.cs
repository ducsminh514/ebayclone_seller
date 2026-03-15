using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.UseCases.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestBuyerController : ControllerBase
    {
        private readonly ICreateTestOrderUseCase _createTestOrderUseCase;
        private readonly EbayClone.Infrastructure.Data.EbayDbContext _db;

        public TestBuyerController(
            ICreateTestOrderUseCase createTestOrderUseCase,
            EbayClone.Infrastructure.Data.EbayDbContext db)
        {
            _createTestOrderUseCase = createTestOrderUseCase;
            _db = db;
        }

        // Endpoint giả lập khách bấm thanh toán (Không cần Auth để dễ test)
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateBuyerTestOrderRequest request)
        {
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
                return Ok(new { Message = "Sản phẩm đã được mua (đơn hàng tạo thành công). Hàng trong kho đã được reserved.", OrderId = orderId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("products")]
        public IActionResult GetAllActiveProducts()
        {
            // Query trực tiếp DB phục vụ test nhanh (bỏ qua Clean Arch cho luồng Test này để tiết kiệm thời gian)
            var products = _db.Products
                .Include(p => p.Variants)
                .Where(p => p.Status == "ACTIVE")
                .ToList();
            return Ok(products);
        }
    }
}
