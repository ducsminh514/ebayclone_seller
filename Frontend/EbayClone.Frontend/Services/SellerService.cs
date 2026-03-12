using System.Net.Http.Json;

namespace EbayClone.Frontend.Services
{
    public class SellerService
    {
        private readonly HttpClient _http;

        public SellerService(HttpClient http)
        {
            _http = http;
        }

        public async Task<SellerFinanceDto?> GetFinanceAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/seller/finance");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SellerFinanceDto>();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class SellerFinanceDto
    {
        public Guid WalletId { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingBalance { get; set; }
        public string Currency { get; set; } = "VND";
        public List<WalletTransactionDto> RecentTransactions { get; set; } = new();
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }
        public string? Description { get; set; }
    }
}
