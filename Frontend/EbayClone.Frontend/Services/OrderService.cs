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

        // ==================== TEST BUYER MOCK ====================

        public async Task<List<EbayClone.Shared.DTOs.Products.ProductDto>> GetTestProductsAsync()
        {
            var response = await _httpClient.GetAsync("api/testbuyer/products");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<EbayClone.Shared.DTOs.Products.ProductDto>>() 
                    ?? new List<EbayClone.Shared.DTOs.Products.ProductDto>();
            }
            throw new Exception("Không thể tải danh sách sản phẩm test.");
        }

        public async Task<string> TestBuyerCheckoutAsync(CreateBuyerTestOrderRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/checkout", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Đặt hàng thành công!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        // ==================== GĐ2: CANCEL REQUEST FLOW ====================

        public async Task<string> BuyerCancelRequestAsync(BuyerCancelRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/cancel-request", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Cancel request đã gửi!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<CancelRequestInfo?> GetCancelRequestAsync(Guid orderId)
        {
            var response = await _httpClient.GetAsync($"api/orders/{orderId}/cancel-request");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CancelRequestInfo>();
            }
            return null;
        }

        public async Task<string> RespondCancelRequestAsync(Guid orderId, RespondCancelRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/orders/{orderId}/cancel-request/respond", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Đã phản hồi!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        /// <summary>
        /// [FIX-F8] Gọi release-funds qua JWT auth, không hardcode internal API key.
        /// </summary>
        public async Task<string> ReleaseFundsAsync()
        {
            var response = await _httpClient.PostAsync("api/orders/release-funds", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception(err);
        }

        // ==================== GĐ5A: RETURN FLOW ====================

        public async Task<ReturnInfo?> GetActiveReturnAsync(Guid orderId)
        {
            var response = await _httpClient.GetAsync($"api/orders/{orderId}/return");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ReturnInfo>();
            return null;
        }

        public async Task<string> OpenReturnAsync(OpenReturnRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/return", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Return request created!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> RespondReturnAsync(Guid returnId, RespondReturnRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/orders/returns/{returnId}/respond", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Đã phản hồi!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> ShipReturnBackAsync(Guid returnId, ShipReturnRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/testbuyer/return/{returnId}/ship", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Buyer đã gửi hàng return!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> IssueRefundAsync(Guid returnId, IssueRefundRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/orders/returns/{returnId}/refund", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Refund đã xử lý!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> RespondPartialOfferAsync(Guid returnId, string buyerDecision)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/orders/returns/{returnId}/respond-partial", new RespondPartialOfferRequest { BuyerDecision = buyerDecision });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Done!";
            }
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception(err);
        }

        // ==================== GĐ5C: DISPUTE FLOW ====================

        public async Task<DisputeInfo?> GetActiveDisputeAsync(Guid orderId)
        {
            var response = await _httpClient.GetAsync($"api/orders/{orderId}/dispute");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<DisputeInfo>();
            return null;
        }

        public async Task<string> OpenDisputeAsync(OpenDisputeRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/testbuyer/dispute", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Dispute opened!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> RespondDisputeAsync(Guid disputeId, RespondDisputeRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/orders/disputes/{disputeId}/respond", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Đã phản hồi dispute!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> EscalateDisputeAsync(Guid disputeId)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/testbuyer/dispute/{disputeId}/escalate", new { });
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Dispute escalated!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }

        public async Task<string> ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/orders/disputes/{disputeId}/resolve", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return result?["message"]?.ToString() ?? "Dispute resolved!";
            }
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(error);
        }
    }

    // ==================== FE DTOs ====================

    public class CancelRequestInfo
    {
        public bool HasPendingRequest { get; set; }
        public Guid? CancellationId { get; set; }
        public string? Reason { get; set; }
        public string? RequestedBy { get; set; }
        public string? Notes { get; set; }
        public DateTimeOffset? RequestedAt { get; set; }
        public DateTimeOffset? ResponseDeadline { get; set; }
    }

    public class ReturnInfo
    {
        public bool HasActiveReturn { get; set; }
        public Guid? ReturnId { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? BuyerMessage { get; set; }
        public string? PhotoUrls { get; set; }
        public string? SellerResponseType { get; set; }
        public string? SellerMessage { get; set; }
        public decimal? RefundAmount { get; set; }
        public decimal? DeductionAmount { get; set; }
        public string? DeductionReason { get; set; }
        public decimal? PartialOfferAmount { get; set; }
        public string? ReturnTrackingCode { get; set; }
        public string? ReturnCarrier { get; set; }
        public string? ReturnShippingPaidBy { get; set; }
        public bool IsStockRestored { get; set; }
        public DateTimeOffset? RequestedAt { get; set; }
        public DateTimeOffset? RespondedAt { get; set; }
        public DateTimeOffset? ReturnShippedAt { get; set; }
        public DateTimeOffset? ReturnReceivedAt { get; set; }
        public DateTimeOffset? RefundedAt { get; set; }
        public DateTimeOffset? SellerResponseDeadline { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    public class DisputeInfo
    {
        public bool HasActiveDispute { get; set; }
        public Guid? DisputeId { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? BuyerMessage { get; set; }
        public string? BuyerEvidenceUrls { get; set; }
        public string? SellerMessage { get; set; }
        public string? SellerEvidenceUrls { get; set; }
        public string? Resolution { get; set; }
        public bool IsDefect { get; set; }
        public DateTimeOffset? OpenedAt { get; set; }
        public DateTimeOffset? SellerRespondedAt { get; set; }
        public DateTimeOffset? EscalatedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public DateTimeOffset? SellerResponseDeadline { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}

