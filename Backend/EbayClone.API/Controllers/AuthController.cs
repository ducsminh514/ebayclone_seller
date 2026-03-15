using EbayClone.Shared.DTOs.Auth;
using EbayClone.Application.UseCases.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRegisterUserUseCase _registerUserUseCase;
        private readonly ILoginUseCase _loginUseCase;
        private readonly IVerifyEmailUseCase _verifyEmailUseCase;
        private readonly IRefreshTokenUseCase _refreshTokenUseCase;
        private readonly IConfiguration _configuration;

        public AuthController(
            IRegisterUserUseCase registerUserUseCase,
            ILoginUseCase loginUseCase,
            IVerifyEmailUseCase verifyEmailUseCase,
            IRefreshTokenUseCase refreshTokenUseCase,
            IConfiguration configuration)
        {
            _registerUserUseCase = registerUserUseCase;
            _loginUseCase = loginUseCase;
            _verifyEmailUseCase = verifyEmailUseCase;
            _refreshTokenUseCase = refreshTokenUseCase;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var userId = await _registerUserUseCase.ExecuteAsync(request);
                return CreatedAtAction(nameof(Register), new { id = userId }, new { Message = "Registration successful. Please check your email for the verification token." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                await _verifyEmailUseCase.ExecuteAsync(request);
                return Ok(new { Message = "Email verified successfully. You can now login." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var loginResult = await _loginUseCase.ExecuteAsync(request);
                
                var authClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, loginResult.UserId),
                    new Claim(ClaimTypes.Name, loginResult.Username),
                    new Claim(ClaimTypes.Role, loginResult.Role),
                    new Claim("HasShop", loginResult.HasShop.ToString()),
                    new Claim("IsVerified", loginResult.IsVerified.ToString()),
                    new Claim("ShopId", loginResult.ShopId?.ToString() ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "superSecretKey@345EbayClone@Authentication123!"));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"] ?? "http://localhost:7094",
                    audience: _configuration["Jwt:Audience"] ?? "http://localhost:7011",
                    expires: DateTime.UtcNow.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Error = ex.Message });
            }
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { Error = "Invalid token claims." });
            }

            try
            {
                var loginResult = await _refreshTokenUseCase.ExecuteAsync(userId);

                // Cấp phát JWT Token MỚI với thông tin mới (Ví dụ: HasShop vữa nãy là false giờ update lên true)
                var authClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, loginResult.UserId),
                    new Claim(ClaimTypes.Name, loginResult.Username),
                    new Claim(ClaimTypes.Role, loginResult.Role),
                    new Claim("HasShop", loginResult.HasShop.ToString()),
                    new Claim("IsVerified", loginResult.IsVerified.ToString()),
                    new Claim("ShopId", loginResult.ShopId?.ToString() ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "superSecretKey@345EbayClone@Authentication123!"));

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"] ?? "http://localhost:7094",
                    audience: _configuration["Jwt:Audience"] ?? "http://localhost:7011",
                    expires: DateTime.UtcNow.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Error = ex.Message });
            }
        }
    }
}
