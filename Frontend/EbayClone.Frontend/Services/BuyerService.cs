using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Shared.DTOs.Products;

namespace EbayClone.Frontend.Services
{
    public class BuyerService
    {
        private readonly HttpClient _httpClient;

        public BuyerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<ProductDto>> GetPublicProductsAsync()
        {
            var response = await _httpClient.GetAsync("api/testbuyer/products");
            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<IEnumerable<ProductDto>>();
                return products ?? Array.Empty<ProductDto>();
            }
            return Array.Empty<ProductDto>();
        }

        public async Task<string> CheckoutAsync(CreateBuyerTestOrderRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/checkout", request);
            if (response.IsSuccessStatusCode)
            {
                return "OK";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi tạo đơn hàng: {error}");
            }
        }
    }
}
