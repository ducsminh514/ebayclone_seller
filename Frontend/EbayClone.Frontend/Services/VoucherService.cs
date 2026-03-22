using System.Net.Http.Json;
using System.Text.Json;
using EbayClone.Shared.DTOs.Vouchers;

namespace EbayClone.Frontend.Services
{
    public class VoucherService
    {
        private readonly HttpClient _httpClient;

        public VoucherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<VoucherDto>> GetVouchersAsync(string? status = null)
        {
            var url = status == null ? "api/vouchers" : $"api/vouchers?status={status}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<VoucherDto>>() ?? new();
        }

        public async Task<VoucherDto> GetByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"api/vouchers/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VoucherDto>() ?? throw new Exception("Not found");
        }

        public async Task<VoucherDto> CreateAsync(CreateVoucherRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/vouchers", request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                var msg = TryExtractMessage(err);
                throw new Exception(msg);
            }
            return await response.Content.ReadFromJsonAsync<VoucherDto>() ?? throw new Exception("Lỗi tạo voucher");
        }

        public async Task UpdateAsync(Guid id, UpdateVoucherRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/vouchers/{id}", request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception(TryExtractMessage(err));
            }
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            var response = await _httpClient.PatchAsJsonAsync($"api/vouchers/{id}/status",
                new UpdateVoucherStatusRequest { Status = status });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception(TryExtractMessage(err));
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"api/vouchers/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception(TryExtractMessage(err));
            }
        }

        /// <summary>Preview discount trước khi checkout (không tốn lượt voucher).</summary>
        public async Task<ApplyVoucherResponse> PreviewDiscountAsync(
            string code, Guid shopId, decimal itemSubtotal, List<Guid>? productIds = null)
        {
            // [FIX-HIGH-4] Dùng typed DTO thay vì anonymous object — type-safe
            var body = new ApplyVoucherPreviewRequest
            {
                Code = code,
                ShopId = shopId,
                ItemSubtotal = itemSubtotal,
                ProductIds = productIds ?? new List<Guid>()
            };
            var response = await _httpClient.PostAsJsonAsync("api/vouchers/apply", body);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new Exception(TryExtractMessage(err));
            }
            return await response.Content.ReadFromJsonAsync<ApplyVoucherResponse>()
                ?? throw new Exception("Lỗi preview voucher");
        }

        private static string TryExtractMessage(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? json;
            }
            catch { }
            return json;
        }
    }
}
