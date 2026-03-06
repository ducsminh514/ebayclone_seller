using System;
using System.Security.Claims;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Products;
using EbayClone.Application.UseCases.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EbayClone.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SELLER")]
    public class ProductsController : ControllerBase
    {
        private readonly ICreateListingUseCase _createListingUseCase;
        private readonly IRestockVariantUseCase _restockVariantUseCase;
        private readonly IGetProductsUseCase _getProductsUseCase;
        private readonly IGetProductByIdUseCase _getProductByIdUseCase;
        private readonly IUpdateProductBasicUseCase _updateProductBasicUseCase;
        private readonly IUpdateProductStatusUseCase _updateProductStatusUseCase;
        private readonly IUpdateProductVariantsUseCase _updateProductVariantsUseCase;
        private readonly ISoftDeleteProductUseCase _softDeleteProductUseCase;

        public ProductsController(
            IGetProductsUseCase getProductsUseCase,
            IGetProductByIdUseCase getProductByIdUseCase,
            ICreateListingUseCase createListingUseCase,
            IRestockVariantUseCase restockVariantUseCase,
            ISoftDeleteProductUseCase softDeleteProductUseCase,
            IUpdateProductBasicUseCase updateProductBasicUseCase,
            IUpdateProductStatusUseCase updateProductStatusUseCase,
            IUpdateProductVariantsUseCase updateProductVariantsUseCase)
        {
            _getProductsUseCase = getProductsUseCase;
            _getProductByIdUseCase = getProductByIdUseCase;
            _createListingUseCase = createListingUseCase;
            _restockVariantUseCase = restockVariantUseCase;
            _softDeleteProductUseCase = softDeleteProductUseCase;
            _updateProductBasicUseCase = updateProductBasicUseCase;
            _updateProductStatusUseCase = updateProductStatusUseCase;
            _updateProductVariantsUseCase = updateProductVariantsUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "You do not have a registered Shop." });
            }

            try
            {
                var products = await _getProductsUseCase.ExecuteAsync(shopId);
                return Ok(products);
            }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "You do not have a registered Shop." });
            }

            try
            {
                var product = await _getProductByIdUseCase.ExecuteAsync(shopId, id);
                if (product == null) return NotFound(new { Error = "Không tìm thấy sản phẩm." });
                return Ok(product);
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateProductBasic(Guid id, [FromBody] UpdateProductBasicRequest request)
        {
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "You do not have a registered Shop." });
            }

            try
            {
                await _updateProductBasicUseCase.ExecuteAsync(shopId, id, request);
                return Ok(new { Message = "Cập nhật sản phẩm thành công." });
            }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateProductStatus(Guid id, [FromBody] UpdateProductStatusRequest request)
        {
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "You do not have a registered Shop." });
            }

            try
            {
                await _updateProductStatusUseCase.ExecuteAsync(shopId, id, request);
                return Ok(new { Message = $"Cập nhật trạng thái sản phẩm sang {request.Status} thành công." });
            }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPost]
        [EnableRateLimiting("create_listing")]
        public async Task<IActionResult> CreateListing([FromBody] CreateListingRequest request)
        {
            // Bảo mật: Lấy ID Shop từ Token Claim, không tin tưởng body request để tránh IDOR
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
            {
                return StatusCode(403, new { Error = "You do not have a registered Shop." });
            }

            try
            {
                var productId = await _createListingUseCase.ExecuteAsync(shopId, request);
                return CreatedAtAction(nameof(CreateListing), new { id = productId }, new { Message = "Sản phẩm mới đã được đưa lên kệ thành công." });
            }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (InvalidOperationException ex) { return UnprocessableEntity(new { Error = ex.Message }); } // 422 = Business rule violation (MonthlyLimit)
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpPut("variants/{variantId:guid}/restock")]
        public async Task<IActionResult> RestockVariant(Guid variantId, [FromBody] RestockVariantRequest request)
        {
            // Lấy ShopId từ JWT claim để verify quyền sở hữu (chống IDOR)
            var shopIdClaim = User.FindFirst("ShopId")?.Value;
            if (string.IsNullOrEmpty(shopIdClaim) || !Guid.TryParse(shopIdClaim, out Guid shopId))
                return StatusCode(403, new { Error = "You do not have a registered Shop." });

            try
            {
                await _restockVariantUseCase.ExecuteAsync(shopId, variantId, request);
                return Ok(new { Message = $"Nhập kho thành công (+{request.AddedQuantity} SP)." });
            }
            catch (ArgumentException ex) { return BadRequest(new { Error = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { Error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDeleteProduct(Guid id)
        {
            try
            {
                var shopIdClaim = User.FindFirst("ShopId")?.Value;
                if (string.IsNullOrEmpty(shopIdClaim))
                    return Unauthorized(new { Error = "Shop context is required." });

                var shopId = Guid.Parse(shopIdClaim);
                await _softDeleteProductUseCase.ExecuteAsync(shopId, id);
                return Ok(new { Message = "Sản phẩm đã được xóa thành công." });
            }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
            catch (ArgumentException ex) { return NotFound(new { Error = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { Error = ex.Message }); }
        }
    }
}
