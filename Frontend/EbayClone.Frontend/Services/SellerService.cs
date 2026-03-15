using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Finance;

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
}
