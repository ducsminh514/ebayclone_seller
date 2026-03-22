using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Feedbacks;
using EbayClone.Shared.DTOs.Common;

namespace EbayClone.Frontend.Services
{
    public class FeedbackService
    {
        private readonly HttpClient _httpClient;

        public FeedbackService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // === Seller endpoints ===

        public async Task<PagedResult<FeedbackDto>?> GetFeedbacksAsync(int page = 1, int pageSize = 10, string? rating = null)
        {
            var url = $"api/feedback?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(rating) && rating != "ALL")
                url += $"&rating={rating}";

            return await _httpClient.GetFromJsonAsync<PagedResult<FeedbackDto>>(url);
        }

        public async Task<FeedbackStatsDto?> GetStatsAsync()
        {
            return await _httpClient.GetFromJsonAsync<FeedbackStatsDto>("api/feedback/stats");
        }

        public async Task<FeedbackDto?> GetFeedbackByOrderAsync(Guid orderId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<FeedbackDto>($"api/feedback/order/{orderId}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> ReplyFeedbackAsync(Guid feedbackId, string reply)
        {
            var request = new ReplyFeedbackRequest { Reply = reply };
            var response = await _httpClient.PostAsJsonAsync($"api/feedback/{feedbackId}/reply", request);
            if (response.IsSuccessStatusCode)
                return "OK";

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        // === Mock Buyer endpoints ===

        public async Task<string> LeaveFeedbackAsync(LeaveFeedbackRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/feedback", request);
            if (response.IsSuccessStatusCode)
                return "OK";

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<FeedbackDto?> CheckFeedbackAsync(Guid orderId)
        {
            try
            {
                // Response: { hasFeedback: bool, feedback: FeedbackDto? }
                var response = await _httpClient.GetAsync($"api/testbuyer/feedback/{orderId}");
                if (!response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadFromJsonAsync<CheckFeedbackResponse>();
                return result?.HasFeedback == true ? result.Feedback : null;
            }
            catch
            {
                return null;
            }
        }

        private class CheckFeedbackResponse
        {
            public bool HasFeedback { get; set; }
            public FeedbackDto? Feedback { get; set; }
        }
    }
}
