using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Common;
using EbayClone.Shared.DTOs.Orders;

namespace EbayClone.Frontend.Services
{
    public class OrderService
    {
        private readonly HttpClient _httpClient;

        public OrderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResult<OrderDto>> GetPagedOrdersAsync(int page = 1, int size = 10, string? status = null, string? search = null)
        {
            var url = $"api/orders?pageNumber={page}&pageSize={size}";
            if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
            if (!string.IsNullOrEmpty(search)) url += $"&searchQuery={Uri.EscapeDataString(search)}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderDto>>();
                return result ?? new PagedResult<OrderDto>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new Exception("Tài khoản của bạn chưa được cấp quyền SELLER. Vui lòng bấm Đăng xuất và Đăng nhập lại để làm mới Token!");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Lỗi máy chủ: {error}");
            }
        }

        public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync()
        {
            var result = await GetPagedOrdersAsync(1, 100); // Temporary legacy support
            return result.Items;
        }

        public async Task<OrderDto> GetOrderByIdAsync(Guid id)
        {
            var response = await _httpClient.GetAsync($"api/orders/{id}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OrderDto>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            throw new Exception($"Không thể tải dữ liệu đơn hàng: {response.StatusCode}");
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/orders/{orderId}/status", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                // error này có thể chứa JSON từ Backend như { "Error": "Không thể SHIPPED nếu chưa qua bước Chuẩn bị hàng." }
                throw new Exception($"{error}");
            }
        }
    }
}
