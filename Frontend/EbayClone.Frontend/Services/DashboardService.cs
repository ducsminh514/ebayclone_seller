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
                return await response.Content.ReadFromJsonAsync<DashboardStatsDto>() ?? throw new InvalidOperationException("Failed to decode dashboard stats.");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API Error ({response.StatusCode}): {errorContent}");
        }
    }
}
