using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EbayClone.Frontend.Services
{
    // ── DTOs chỉ dùng ở FE (không share với Domain) ──────────────────────

    public class WalletSummaryDto
    {
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance   { get; set; }
        public decimal OnHoldBalance    { get; set; }
        public decimal TotalBalance     { get; set; }
        public string  Currency         { get; set; } = "VND";
        public string  SellerLevel      { get; set; } = "NEW";
        public int     HoldDays         { get; set; } = 21;
        public int     TotalTransactions { get; set; }
        public int     DefectCount      { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public bool    IsNegativeBalance { get; set; }
        public DateTimeOffset? WalletUpdatedAt { get; set; }
        // Bank info (masked)
        public string? BankName               { get; set; }
        public string? BankAccountMasked      { get; set; }
        public string? BankVerificationStatus { get; set; }
    }

    public class TransactionItemDto
    {
        public Guid    Id           { get; set; }
        public decimal Amount       { get; set; }
        public string  Type         { get; set; } = "";
        public string  Status       { get; set; } = "";
        public string? OrderNumber  { get; set; }
        public string? Description  { get; set; }
        public decimal BalanceAfter { get; set; }
        public Guid?   ReferenceId  { get; set; }
        public string? ReferenceType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class WalletPagedResult
    {
        public System.Collections.Generic.List<TransactionItemDto> Items { get; set; } = new();
        public int Total     { get; set; }
        public int Page      { get; set; }
        public int PageSize  { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Backend trả { message, remainingAvailable } khi success
    /// hoặc { error } khi fail (camelCase).
    /// [JsonPropertyName] để map đúng với JSON camelCase của ASP.NET Core.
    /// </summary>
    public class PayoutResultDto
    {
        [JsonPropertyName("message")]
        public string?  Message            { get; set; }

        [JsonPropertyName("remainingAvailable")]
        public decimal  RemainingAvailable { get; set; }

        [JsonPropertyName("error")]
        public string?  Error              { get; set; }
    }

    // ── Service ──────────────────────────────────────────────────────────

    public class WalletService
    {
        private readonly HttpClient _http;

        public WalletService(HttpClient httpClient)
        {
            _http = httpClient;
        }

        /// <summary>GET /api/wallet — 3 balances + SellerLevel + metrics</summary>
        public async Task<WalletSummaryDto?> GetSummaryAsync()
        {
            try
            {
                var res = await _http.GetAsync("api/wallet");
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<WalletSummaryDto>();

                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    return null;

                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Wallet API lỗi ({res.StatusCode}): {err}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[WalletService] GetSummary error: {ex.Message}");
                return null;
            }
        }

        /// <summary>GET /api/wallet/transactions — paged, filter by type &amp; date</summary>
        public async Task<WalletPagedResult> GetTransactionsAsync(
            int page = 1,
            int pageSize = 10,
            string? type = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null)
        {
            var url = $"api/wallet/transactions?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(type)) url += $"&type={Uri.EscapeDataString(type)}";
            if (from.HasValue) url += $"&from={Uri.EscapeDataString(from.Value.ToString("o"))}";
            if (to.HasValue)   url += $"&to={Uri.EscapeDataString(to.Value.ToString("o"))}";

            try
            {
                var res = await _http.GetAsync(url);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadFromJsonAsync<WalletPagedResult>()
                           ?? new WalletPagedResult();

                return new WalletPagedResult();
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[WalletService] GetTransactions error: {ex.Message}");
                return new WalletPagedResult();
            }
        }

        /// <summary>POST /api/wallet/payout — mock withdraw</summary>
        public async Task<PayoutResultDto> RequestPayoutAsync(decimal amount)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("api/wallet/payout", new { Amount = amount });
                var content = await res.Content.ReadFromJsonAsync<PayoutResultDto>()
                              ?? new PayoutResultDto();

                if (!res.IsSuccessStatusCode)
                {
                    // backend trả { error: "..." } khi fail
                    content.Message = null;
                }
                return content;
            }
            catch (Exception ex)
            {
                return new PayoutResultDto { Error = ex.Message };
            }
        }
    }
}
