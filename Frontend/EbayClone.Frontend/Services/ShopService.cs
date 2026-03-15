using System.Net.Http.Json;
using EbayClone.Shared.DTOs.Shops;
using EbayClone.Shared.DTOs.Common;

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
            // 409 Conflict = Shop already exists → NOT an error, just advance
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return "Shop already exists. Continuing...";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "An error occurred while creating the shop.");
            }
        }

        public async Task<string> VerifyOtpAsync(VerifyShopOtpRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/shops/kyc/verify", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VerifyOtpResponse>();
                return result?.Message ?? "Verified successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "An error occurred while verifying OTP.");
            }
        }

        public async Task<string> LinkBankAccountAsync(LinkBankAccountRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/shops/payments/link", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
                return result?.Message ?? "Bank details saved.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to link bank account.");
            }
        }

        public async Task<string> VerifyMicroDepositAsync(VerifyMicroDepositRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/shops/payments/verify", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VerifyOtpResponse>();
                return result?.Message ?? "Bank verified successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Micro-deposit verification failed.");
            }
        }

        public async Task<OnboardingStatusResponse?> GetOnboardingStatusAsync()
        {
            var response = await _httpClient.GetAsync("api/shops/onboarding/status");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null; // User chưa có shop → Onboarding ở step 0
            if (!response.IsSuccessStatusCode)
                return null; // 401 hoặc lỗi khác → xử lý gracefully
            return await response.Content.ReadFromJsonAsync<OnboardingStatusResponse>();
        }

        public async Task<ShopProfileResponse> GetShopProfileAsync()
        {
            return await _httpClient.GetFromJsonAsync<ShopProfileResponse>("api/shops/profile")
                   ?? new ShopProfileResponse();
        }

        public async Task<string> UpdateShopProfileAsync(UpdateShopProfileRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync("api/shops/profile", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();
                return result?.Message ?? "Store profile updated successfully.";
            }
            else
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Failed to update store profile.");
            }
        }

        /// <summary>
        /// [DEV ONLY] Xóa shop + wallet để reset onboarding flow.
        /// </summary>
        public async Task DevResetOnboardingAsync()
        {
            var response = await _httpClient.DeleteAsync("api/shops/dev/reset");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                throw new InvalidOperationException(error?.Error ?? "Reset failed.");
            }
        }
    }
}
