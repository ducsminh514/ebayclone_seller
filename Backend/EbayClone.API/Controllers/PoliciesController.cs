using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Policies;
using EbayClone.Application.UseCases.Policies;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliciesController : ControllerBase
    {
        private readonly ICreateShippingPolicyUseCase _createShippingPolicyUseCase;
        private readonly ICreateReturnPolicyUseCase _createReturnPolicyUseCase;

        public PoliciesController(ICreateShippingPolicyUseCase createShippingPolicyUseCase, ICreateReturnPolicyUseCase createReturnPolicyUseCase)
        {
            _createShippingPolicyUseCase = createShippingPolicyUseCase;
            _createReturnPolicyUseCase = createReturnPolicyUseCase;
        }

        /// <summary>
        /// Tạo mới Shipping Policy cho Shop
        /// LƯU Ý BẢO MẬT: ShopId phải được trích xuất từ JWT Token, tuyệt đối không lấy từ Body client gửi lên.
        /// </summary>
        [HttpPost("shipping")]
        public async Task<IActionResult> CreateShippingPolicy([FromBody] CreateShippingPolicyRequest request)
        {
            // Tạm thời mock ShopId bảo mật từ Token
            var mockShopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            try
            {
                var policyId = await _createShippingPolicyUseCase.ExecuteAsync(mockShopId, request);
                return CreatedAtAction(nameof(GetShippingPolicies), new { id = policyId }, new { Id = policyId, Message = "Shipping Policy created successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Hành vi đạt giới hạn > 100 policies
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới Return Policy cho Shop
        /// </summary>
        [HttpPost("return")]
        public async Task<IActionResult> CreateReturnPolicy([FromBody] CreateReturnPolicyRequest request)
        {
            var mockShopId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            try
            {
                var policyId = await _createReturnPolicyUseCase.ExecuteAsync(mockShopId, request);
                return CreatedAtAction(nameof(GetReturnPolicies), new { id = policyId }, new { Id = policyId, Message = "Return Policy created successfully." });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("shipping")]
        public IActionResult GetShippingPolicies()
        {
            return Ok(new { Message = "List of shipping policies" });
        }

        [HttpGet("return")]
        public IActionResult GetReturnPolicies()
        {
            return Ok(new { Message = "List of return policies" });
        }
    }
}
