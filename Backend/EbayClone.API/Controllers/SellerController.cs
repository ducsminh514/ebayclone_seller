using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using EbayClone.Application.UseCases.Finance;
using System.Security.Claims;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/seller")]
    public class SellerController : ControllerBase
    {
        private readonly IGetSellerFinanceUseCase _getSellerFinanceUseCase;

        public SellerController(IGetSellerFinanceUseCase getSellerFinanceUseCase)
        {
            _getSellerFinanceUseCase = getSellerFinanceUseCase;
        }

        [HttpGet("finance")]
        public async Task<IActionResult> GetFinance()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _getSellerFinanceUseCase.ExecuteAsync(userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
