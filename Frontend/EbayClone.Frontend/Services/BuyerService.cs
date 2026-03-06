using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

// DTO from API side
namespace EbayClone.Frontend.Services
{
    public class CreateBuyerTestOrderRequest
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string ReceiverInfo { get; set; } = string.Empty;
    }

    public class BuyerService
    {
        private readonly HttpClient _httpClient;

        public BuyerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<EbayClone.Domain.Entities.Product>> GetPublicProductsAsync()
        {
            var response = await _httpClient.GetAsync("api/testbuyer/products");
            if (response.IsSuccessStatusCode)
            {
                var products = await response.Content.ReadFromJsonAsync<IEnumerable<EbayClone.Domain.Entities.Product>>();
                return products ?? Array.Empty<EbayClone.Domain.Entities.Product>();
            }
            return Array.Empty<EbayClone.Domain.Entities.Product>();
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
