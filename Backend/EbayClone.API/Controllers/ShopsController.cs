using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Application.UseCases.Shops;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.Interfaces;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ShopsController : ControllerBase
    {
        private readonly ICreateShopUseCase _createShopUseCase;
        private readonly IVerifyShopOtpUseCase _verifyShopOtpUseCase;
        private readonly ILinkBankAccountUseCase _linkBankAccountUseCase;
        private readonly IVerifyMicroDepositUseCase _verifyMicroDepositUseCase;
        private readonly IUpdateShopProfileUseCase _updateShopProfileUseCase;
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ShopsController(
            ICreateShopUseCase createShopUseCase,
            IVerifyShopOtpUseCase verifyShopOtpUseCase,
            ILinkBankAccountUseCase linkBankAccountUseCase,
            IVerifyMicroDepositUseCase verifyMicroDepositUseCase,
            IUpdateShopProfileUseCase updateShopProfileUseCase,
            IShopRepository shopRepository,
            ISellerWalletRepository walletRepository,
            IUnitOfWork unitOfWork)
        {
            _createShopUseCase = createShopUseCase;
            _verifyShopOtpUseCase = verifyShopOtpUseCase;
            _linkBankAccountUseCase = linkBankAccountUseCase;
            _verifyMicroDepositUseCase = verifyMicroDepositUseCase;
            _updateShopProfileUseCase = updateShopProfileUseCase;
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Người bán gửi yêu cầu mở Shop (Bước 1 & 2 - Chờ Xác Minh OTP)
        /// </summary>
        [HttpPost("api/shops/kyc")]
        public async Task<IActionResult> CreateShop([FromBody] CreateShopRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            try
            {
                var shopId = await _createShopUseCase.ExecuteAsync(userId, request);
                return CreatedAtAction(nameof(GetShopById), new { id = shopId }, new { Id = shopId, Message = "Shop created. Pending OTP verification." });
            }
            catch (DbUpdateException ex)
            {
                // Bắt lỗi Unique Index (1 User - 1 Shop) từ Data Engine
                return Conflict(new { Error = "Conflict! User already has a shop or Data is duplicated.", Details = ex.InnerException?.Message });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("User already owns a shop"))
                {
                    return Conflict(new { Error = ex.Message });
                }
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Xác minh Shop bằng mã OTP ảo (Bước 3 MVP)
        /// </summary>
        [HttpPost("api/shops/kyc/verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyShopOtpRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access. Invalid or missing user token." });
            }

            try
            {
                var result = await _verifyShopOtpUseCase.ExecuteAsync(userId, request);
                return Ok(new { Success = result, Message = "Xác nhận OTP thành công! Danh tính của bạn đã được xác minh. Tiếp theo: Liên kết ngân hàng." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Liên kết tài khoản ngân hàng để nhận tiền (Bước 4 - Managed Payments)
        /// </summary>
        [HttpPost("api/shops/payments/link")]
        public async Task<IActionResult> LinkBankAccount([FromBody] LinkBankAccountRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            try
            {
                await _linkBankAccountUseCase.ExecuteAsync(userId, request);
                return Ok(new { Message = "Bank details saved. Please check your bank statement in 1-2 days for two small deposits." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Xác minh tiền lẻ (Micro-deposits) để hoàn tất Managed Payments
        /// </summary>
        [HttpPost("api/shops/payments/verify")]
        public async Task<IActionResult> VerifyMicroDeposit([FromBody] VerifyMicroDepositRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            try
            {
                var result = await _verifyMicroDepositUseCase.ExecuteAsync(userId, request);
                return Ok(new { Success = result, Message = "Xác minh tài khoản ngân hàng thành công! Bạn hiện đã có thể đăng bán sản phẩm." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy trạng thái Onboarding hiện tại của Shop
        /// </summary>
        [HttpGet("api/shops/onboarding/status")]
        public async Task<IActionResult> GetOnboardingStatus()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null) return NotFound(new { Error = "Shop not found." });

            return Ok(new
            {
                shop.IsIdentityVerified,
                shop.BankVerificationStatus,
                shop.IsVerified,
                shop.IsPolicyOptedIn,
                shop.MonthlyListingLimit,
                shop.BankVerificationAttempts
            });
        }

        // Placeholder for created at result
        [HttpGet("api/shops/{id}")]
        public IActionResult GetShopById(Guid id)
        {
            return Ok(new { Id = id }); // Mock return
        }

        /// <summary>
        /// Cập nhật Store Profile (Name, Description, Avatar, Banner).
        /// eBay thật: Truy cập qua Store tab > Edit store.
        /// </summary>
        [HttpPut("api/shops/profile")]
        public async Task<IActionResult> UpdateShopProfile([FromBody] UpdateShopProfileRequest request)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            try
            {
                await _updateShopProfileUseCase.ExecuteAsync(userId, request);
                return Ok(new { Message = "Store profile updated successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thông tin Store Profile hiện tại để hiển thị trên trang Edit.
        /// </summary>
        [HttpGet("api/shops/profile")]
        public async Task<IActionResult> GetShopProfile()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null) return NotFound(new { Error = "Shop not found." });

            return Ok(new
            {
                shop.Id,
                shop.Name,
                shop.Description,
                shop.AvatarUrl,
                shop.BannerUrl,
                shop.Address,
                shop.CreatedAt
            });
        }
        /// <summary>
        /// [DEV ONLY] Reset toàn bộ onboarding data cho user hiện tại để test lại flow từ đầu.
        /// Xóa Shop + SellerWallet. KHÔNG dùng cho production!
        /// </summary>
        [HttpDelete("api/shops/dev/reset")]
        public async Task<IActionResult> DevResetOnboarding()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized access." });
            }

            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null) return NotFound(new { Error = "No shop found to reset." });

            try
            {
                // Xóa wallet trước (FK constraint)
                var wallet = await _walletRepository.GetByShopIdAsync(shop.Id);
                if (wallet != null)
                {
                    // Dùng DbContext trực tiếp để xóa vì Repository không có Delete method
                    var dbContext = HttpContext.RequestServices.GetRequiredService<EbayClone.Infrastructure.Data.EbayDbContext>();
                    dbContext.SellerWallets.Remove(wallet);
                    dbContext.Shops.Remove(shop);
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    var dbContext = HttpContext.RequestServices.GetRequiredService<EbayClone.Infrastructure.Data.EbayDbContext>();
                    dbContext.Shops.Remove(shop);
                    await dbContext.SaveChangesAsync();
                }

                return Ok(new { Message = "Onboarding reset successfully. Please log out and log back in." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Reset failed: {ex.Message}" });
            }
        }
    }
}
