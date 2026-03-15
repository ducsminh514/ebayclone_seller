using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Common;
using EbayClone.Shared.DTOs.Policies;

namespace EbayClone.Frontend.Services
{
    public class PolicyService
    {
        private readonly HttpClient _httpClient;

        public PolicyService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateShippingPolicyAsync(CreateShippingPolicyRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/policies/shipping", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PolicyCreationResponse>();
                return result?.Message ?? "Shipping policy created successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to create shipping policy.");
            }
        }

        public async Task<string> CreateReturnPolicyAsync(CreateReturnPolicyRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/policies/return", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PolicyCreationResponse>();
                return result?.Message ?? "Return policy created successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to create return policy.");
            }
        }
        public async Task<IEnumerable<ShippingPolicyDto>> GetShippingPoliciesAsync()
        {
            var response = await _httpClient.GetAsync("api/policies/shipping");
            if (response.IsSuccessStatusCode)
            {
                var policies = await response.Content.ReadFromJsonAsync<IEnumerable<ShippingPolicyDto>>();
                return policies ?? Array.Empty<ShippingPolicyDto>();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Lỗi tải dữ liệu Shipping Policies từ máy chủ.");
            }
        }

        public async Task<IEnumerable<ReturnPolicyDto>> GetReturnPoliciesAsync()
        {
            var response = await _httpClient.GetAsync("api/policies/return");
            if (response.IsSuccessStatusCode)
            {
                var policies = await response.Content.ReadFromJsonAsync<IEnumerable<ReturnPolicyDto>>();
                return policies ?? Array.Empty<ReturnPolicyDto>();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Lỗi tải dữ liệu Return Policies từ máy chủ.");
            }
        }

        public async Task<string> CreatePaymentPolicyAsync(CreatePaymentPolicyRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/policies/payment", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PolicyCreationResponse>();
                return result?.Message ?? "Payment policy created successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to create payment policy.");
            }
        }

        public async Task<IEnumerable<PaymentPolicyDto>> GetPaymentPoliciesAsync()
        {
            var response = await _httpClient.GetAsync("api/policies/payment");
            if (response.IsSuccessStatusCode)
            {
                var policies = await response.Content.ReadFromJsonAsync<IEnumerable<PaymentPolicyDto>>();
                return policies ?? Array.Empty<PaymentPolicyDto>();
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Lỗi tải dữ liệu Payment Policies từ máy chủ.");
            }
        }

        public async Task<string> DeletePolicyAsync(string policyType, Guid policyId)
        {
            var response = await _httpClient.DeleteAsync($"api/policies/{policyType}/{policyId}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
                return result?.Message ?? "Policy deleted.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to delete policy.");
            }
        }

        public async Task<string> SetDefaultPolicyAsync(string policyType, Guid policyId)
        {
            var response = await _httpClient.PutAsync($"api/policies/{policyType}/{policyId}/set-default", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
                return result?.Message ?? "Policy set as default.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to set default policy.");
            }
        }

        public async Task<string> OptInPoliciesAsync()
        {
            var response = await _httpClient.PostAsync("api/policies/opt-in", null);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
                return result?.Message ?? "Opted in successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to opt in.");
            }
        }
    }
}
