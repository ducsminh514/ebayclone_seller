using System.Net.Http.Json;
using EbayClone.Application.DTOs.Shops;

namespace EbayClone.Frontend.Services
{
    public class ShopService
    {
        private readonly HttpClient _httpClient;

        public ShopService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateShopAsync(CreateShopRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/shops/kyc", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ShopCreationResponse>();
                return result?.Message ?? "Shop created successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "An error occurred while creating the shop.");
            }
        }
    }

    public class ShopCreationResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
