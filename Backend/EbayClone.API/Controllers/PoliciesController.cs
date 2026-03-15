using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Policies;
using EbayClone.Application.UseCases.Policies;
using Microsoft.AspNetCore.RateLimiting;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PoliciesController : ControllerBase
    {
        private readonly ICreateShippingPolicyUseCase _createShippingPolicyUseCase;
        private readonly ICreateReturnPolicyUseCase _createReturnPolicyUseCase;
        private readonly ICreatePaymentPolicyUseCase _createPaymentPolicyUseCase;
        private readonly IGetShippingPoliciesUseCase _getShippingPoliciesUseCase;
        private readonly IGetReturnPoliciesUseCase _getReturnPoliciesUseCase;
        private readonly IGetPaymentPoliciesUseCase _getPaymentPoliciesUseCase;
        private readonly IDeletePolicyUseCase _deletePolicyUseCase;
        private readonly ISetDefaultPolicyUseCase _setDefaultPolicyUseCase;
        private readonly IOptInPolicyUseCase _optInPolicyUseCase;
        private readonly EbayClone.Application.Interfaces.Repositories.IShopRepository _shopRepository;

        public PoliciesController(
            ICreateShippingPolicyUseCase createShippingPolicyUseCase, 
            ICreateReturnPolicyUseCase createReturnPolicyUseCase, 
            ICreatePaymentPolicyUseCase createPaymentPolicyUseCase,
            IGetShippingPoliciesUseCase getShippingPoliciesUseCase,
            IGetReturnPoliciesUseCase getReturnPoliciesUseCase,
            IGetPaymentPoliciesUseCase getPaymentPoliciesUseCase,
            IDeletePolicyUseCase deletePolicyUseCase,
            ISetDefaultPolicyUseCase setDefaultPolicyUseCase,
            IOptInPolicyUseCase optInPolicyUseCase,
            EbayClone.Application.Interfaces.Repositories.IShopRepository shopRepository)
        {
            _createShippingPolicyUseCase = createShippingPolicyUseCase;
            _createReturnPolicyUseCase = createReturnPolicyUseCase;
            _createPaymentPolicyUseCase = createPaymentPolicyUseCase;
            _getShippingPoliciesUseCase = getShippingPoliciesUseCase;
            _getReturnPoliciesUseCase = getReturnPoliciesUseCase;
            _getPaymentPoliciesUseCase = getPaymentPoliciesUseCase;
            _deletePolicyUseCase = deletePolicyUseCase;
            _setDefaultPolicyUseCase = setDefaultPolicyUseCase;
            _optInPolicyUseCase = optInPolicyUseCase;
            _shopRepository = shopRepository;
        }

        /// <summary>
        /// Tạo mới Shipping Policy cho Shop
        /// LƯU Ý BẢO MẬT: ShopId phải được trích xuất từ JWT Token, tuyệt đối không lấy từ Body client gửi lên.
        /// </summary>
        [HttpPost("shipping")]
        [EnableRateLimiting("strict_policy")]
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

            if (!shop.IsVerified)
            {
                return Forbid("Shop must be verified before creating business policies.");
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
        [EnableRateLimiting("strict_policy")]
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

            if (!shop.IsVerified)
            {
                return Forbid("Shop must be verified before creating business policies.");
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

        /// <summary>
        /// Tạo mới Payment Policy cho Shop
        /// </summary>
        [HttpPost("payment")]
        [EnableRateLimiting("strict_policy")]
        public async Task<IActionResult> CreatePaymentPolicy([FromBody] CreatePaymentPolicyRequest request)
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

            if (!shop.IsVerified)
            {
                return Forbid("Shop must be verified before creating business policies.");
            }

            try
            {
                var policyId = await _createPaymentPolicyUseCase.ExecuteAsync(shop.Id, request);
                return CreatedAtAction(nameof(GetPaymentPolicies), new { id = policyId }, new { Id = policyId, Message = "Payment Policy created successfully." });
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
        [HttpGet("payment")]
        public async Task<IActionResult> GetPaymentPolicies()
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

            var policies = await _getPaymentPoliciesUseCase.ExecuteAsync(shop.Id);
            return Ok(policies);
        }

        /// <summary>
        /// Xóa policy theo loại (shipping/return/payment) và ID.
        /// SECURITY: Validate ownership qua JWT shopId.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpDelete("{policyType}/{policyId}")]
        public async Task<IActionResult> DeletePolicy(string policyType, Guid policyId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            try
            {
                await _deletePolicyUseCase.ExecuteAsync(shop.Id, policyId, policyType);
                return Ok(new { Message = $"{policyType} policy deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Set 1 policy làm default cho loại tương ứng (atomic: clear old + set new).
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPut("{policyType}/{policyId}/set-default")]
        public async Task<IActionResult> SetDefaultPolicy(string policyType, Guid policyId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null)
            {
                return BadRequest(new { Error = "User does not have an active shop." });
            }

            try
            {
                await _setDefaultPolicyUseCase.ExecuteAsync(shop.Id, policyId, policyType);
                return Ok(new { Message = $"{policyType} policy set as default." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Opt-in vào Business Policies (eBay thật: bizpolicy.ebay.com/policyoptin).
        /// </summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("opt-in")]
        public async Task<IActionResult> OptInPolicies()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            try
            {
                await _optInPolicyUseCase.ExecuteAsync(userId);
                return Ok(new { Message = "Successfully opted into Business Policies." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
