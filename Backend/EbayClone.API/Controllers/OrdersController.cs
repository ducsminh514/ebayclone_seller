using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Orders;
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
        public async Task<IActionResult> GetShopOrders()
        {
            var shopId = GetShopId();
            if (!shopId.HasValue)
            {
                return StatusCode(403, new { Error = "Account not authorized. HasShop claim missing. Please relogin to refresh claims." });
            }

            var orders = await _getOrdersUseCase.ExecuteAsync(shopId.Value);
            return Ok(orders);
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
    }
}
