using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EbayClone.API.Controllers
{
    /// <summary>
    /// API endpoints cho Seller Wallet Dashboard.
    /// GET  /api/wallet              → Số dư + SellerLevel info
    /// GET  /api/wallet/transactions → Paged transaction history, filter type/date
    /// POST /api/wallet/payout       → Mock on-demand payout (ghi WITHDRAW log)
    ///
    /// Security: Yêu cầu [Authorize] — lấy ShopId từ JWT userId.
    /// IDOR phòng chống: luôn lấy shop qua userId (không nhận shopId từ request).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/wallet")]
    public class WalletController : ControllerBase
    {
        private readonly IGetSellerFinanceUseCase _financeUseCase;
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _transactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public WalletController(
            IGetSellerFinanceUseCase financeUseCase,
            IShopRepository shopRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository transactionRepository,
            IUnitOfWork unitOfWork)
        {
            _financeUseCase = financeUseCase;
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("sub");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
                throw new UnauthorizedAccessException("Không xác định được User ID.");
            return id;
        }

        // ──────────────────────────────────────────────────────────────
        // GET /api/wallet
        // Trả về: 3 balances, SellerLevel, HoldDays, shop metrics
        // ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetWalletSummary(CancellationToken cancellationToken)
        {
            try
            {
                var userId = GetCurrentUserId();
                var financeDto = await _financeUseCase.ExecuteAsync(userId, cancellationToken);
                var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);

                // Lấy wallet để lấy UpdatedAt (GAP-2). Shop có thể null nếu chưa tạo shop.
                var walletInfo = shop != null
                    ? await _walletRepository.GetByShopIdAsync(shop.Id, cancellationToken)
                    : null;

                return Ok(new
                {
                    financeDto.AvailableBalance,
                    financeDto.PendingBalance,
                    financeDto.OnHoldBalance,
                    TotalBalance = financeDto.AvailableBalance + financeDto.PendingBalance + financeDto.OnHoldBalance,
                    financeDto.Currency,
                    financeDto.WalletId,
                    // GAP-2: Thêm UpdatedAt — FE hiển thị "Cập nhật lúc..."
                    WalletUpdatedAt = walletInfo?.UpdatedAt,
                    // SellerLevel & hold info
                    SellerLevel   = shop?.SellerLevel ?? "NEW",
                    HoldDays      = shop?.GetHoldDays() ?? 21,
                    // Shop performance metrics
                    TotalTransactions = shop?.TotalTransactions ?? 0,
                    DefectCount       = shop?.DefectCount ?? 0,
                    TotalSalesAmount  = shop?.TotalSalesAmount ?? 0,
                    // GAP-6: Bank info (masked) cho Payout Section
                    BankName               = shop?.BankName,
                    BankAccountMasked      = MaskAccount(shop?.BankAccountNumber),
                    BankVerificationStatus = shop?.BankVerificationStatus,
                    // Cảnh báo ví âm
                    IsNegativeBalance = financeDto.AvailableBalance < 0
                });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
            catch (InvalidOperationException ex)   { return NotFound(new { error = ex.Message }); }
        }

        private static string? MaskAccount(string? accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return accountNumber;
            return $"***{accountNumber[^4..]}"; // chỉ hiển thị 4 số cuối
        }

        // ──────────────────────────────────────────────────────────────
        // GET /api/wallet/transactions
        // Query: page (default 1), pageSize (default 10, max 100),
        //        type (ORDER_INCOME | ESCROW_RELEASE | REFUND |
        //              PLATFORM_FEE | WITHDRAW | DISPUTE_HOLD),
        //        from, to (ISO 8601 DateTimeOffset)
        // ──────────────────────────────────────────────────────────────
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? type = null,
            [FromQuery] DateTimeOffset? from = null,
            [FromQuery] DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (page < 1) page = 1;
                pageSize = Math.Clamp(pageSize, 1, 100);

                var userId = GetCurrentUserId();
                var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
                if (shop == null) return NotFound(new { error = "Shop not found." });

                var wallet = await _walletRepository.GetByShopIdAsync(shop.Id, cancellationToken);
                if (wallet == null) return NotFound(new { error = "Wallet not initialized." });

                var (items, total) = await _transactionRepository.GetPagedAsync(
                    shop.Id, page, pageSize, type, from, to, cancellationToken);

                return Ok(new
                {
                    Items = items.Select(t => new
                    {
                        t.Id,
                        t.Amount,
                        t.Type,
                        t.Status,
                        // GAP-1: Trả OrderNumber — FE hiển thị "Đơn #EB1234" trong bảng transaction
                        t.OrderNumber,
                        t.Description,
                        t.BalanceAfter,
                        t.ReferenceId,
                        t.ReferenceType,
                        t.CreatedAt
                    }),
                    Total      = total,
                    Page       = page,
                    PageSize   = pageSize,
                    TotalPages = (int)Math.Ceiling((double)total / pageSize)
                });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        // ──────────────────────────────────────────────────────────────
        // POST /api/wallet/payout
        // Body: { "amount": 500000 }
        // Mock: trừ AvailableBalance + ghi WITHDRAW transaction
        // Production: cần kết nối banking/payment gateway API
        // ──────────────────────────────────────────────────────────────
        [HttpPost("payout")]
        public async Task<IActionResult> RequestPayout(
            [FromBody] PayoutRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.Amount <= 0)
                    return BadRequest(new { error = "Số tiền rút phải lớn hơn 0." });

                var userId = GetCurrentUserId();
                var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
                if (shop == null) return NotFound(new { error = "Shop not found." });

                var wallet = await _walletRepository.GetByShopIdAsync(shop.Id, cancellationToken);
                if (wallet == null) return NotFound(new { error = "Wallet not initialized." });

                if (wallet.AvailableBalance < request.Amount)
                    return BadRequest(new
                    {
                        error = $"Số dư khả dụng không đủ. Hiện có: {wallet.AvailableBalance:N0} đ."
                    });

                // Ghi vào ví
                wallet.Withdraw(request.Amount);
                _walletRepository.Update(wallet);

                // Ghi WalletTransaction WITHDRAW
                await _transactionRepository.AddAsync(new Domain.Entities.WalletTransaction
                {
                    ShopId        = shop.Id,
                    Amount        = -request.Amount,
                    Type          = "WITHDRAW",
                    ReferenceType = "PAYOUT",
                    Description   = $"Rút tiền về ngân hàng (mock). Số tiền: {request.Amount:N0} đ",
                    BalanceAfter  = wallet.TotalBalance
                }, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Ok(new
                {
                    message            = $"Đã gửi yêu cầu rút {request.Amount:N0} đ. Tiền sẽ về trong 1-3 ngày làm việc (mock).",
                    remainingAvailable = wallet.AvailableBalance
                });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
            catch (InvalidOperationException ex)   { return BadRequest(new { error = ex.Message }); }
        }

        public record PayoutRequest(decimal Amount);
    }
}
