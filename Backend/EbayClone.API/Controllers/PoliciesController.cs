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
        private readonly IGetShippingPoliciesUseCase _getShippingPoliciesUseCase;
        private readonly IGetReturnPoliciesUseCase _getReturnPoliciesUseCase;
        private readonly EbayClone.Application.Interfaces.Repositories.IShopRepository _shopRepository;

        public PoliciesController(
            ICreateShippingPolicyUseCase createShippingPolicyUseCase, 
            ICreateReturnPolicyUseCase createReturnPolicyUseCase, 
            IGetShippingPoliciesUseCase getShippingPoliciesUseCase,
            IGetReturnPoliciesUseCase getReturnPoliciesUseCase,
            EbayClone.Application.Interfaces.Repositories.IShopRepository shopRepository)
        {
            _createShippingPolicyUseCase = createShippingPolicyUseCase;
            _createReturnPolicyUseCase = createReturnPolicyUseCase;
            _getShippingPoliciesUseCase = getShippingPoliciesUseCase;
            _getReturnPoliciesUseCase = getReturnPoliciesUseCase;
            _shopRepository = shopRepository;
        }

        /// <summary>
        /// Tạo mới Shipping Policy cho Shop
        /// LƯU Ý BẢO MẬT: ShopId phải được trích xuất từ JWT Token, tuyệt đối không lấy từ Body client gửi lên.
        /// </summary>
        [HttpPost("shipping")]
        public async Task<IActionResult> CreateShippingPolicy([FromBody] CreateShippingPolicyRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            try
            {
                var policyId = await _createShippingPolicyUseCase.ExecuteAsync(shop.Id, request);
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
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            try
            {
                var policyId = await _createReturnPolicyUseCase.ExecuteAsync(shop.Id, request);
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

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("shipping")]
        public async Task<IActionResult> GetShippingPolicies()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            var policies = await _getShippingPoliciesUseCase.ExecuteAsync(shop.Id);
            return Ok(policies);
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("return")]
        public async Task<IActionResult> GetReturnPolicies()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            var policies = await _getReturnPoliciesUseCase.ExecuteAsync(shop.Id);
            return Ok(policies);
        }
    }
}
