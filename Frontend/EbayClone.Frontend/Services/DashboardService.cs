using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Dashboard;

namespace EbayClone.Frontend.Services
{
    public class DashboardService
    {
        private readonly HttpClient _httpClient;

        public DashboardService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var response = await _httpClient.GetAsync("api/dashboard/stats");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DashboardStatsDto>() ?? new DashboardStatsDto();
            }

            // 401/403: User chưa đăng nhập hoặc token hết hạn → trả DTO rỗng thay vì crash
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                Console.WriteLine("Dashboard: User not authenticated, returning empty stats.");
                return new DashboardStatsDto();
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Error ({response.StatusCode}): {errorContent}");
        }
    }
}
