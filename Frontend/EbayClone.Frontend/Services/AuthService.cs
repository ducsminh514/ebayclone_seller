using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using EbayClone.Shared.DTOs.Auth;
using EbayClone.Shared.DTOs.Common;

namespace EbayClone.Frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return result?.Message ?? "Registration successful.";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(rawError, errorOpt);
                    throw new InvalidOperationException(errorObj?.Error ?? "An error occurred during registration.");
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new InvalidOperationException($"Backend Error 500: {rawError}");
                }
            }
        }

        public async Task<string> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/verify-email", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                return result?.Message ?? "Email verified successfully.";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(rawError, errorOpt);
                    throw new InvalidOperationException(errorObj?.Error ?? "An error occurred during verification.");
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new InvalidOperationException($"Backend Error 500: {rawError}");
                }
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>() ?? new LoginResponse();
                await _localStorage.SetItemAsync("authToken", result.Token);
                
                ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(result.Token);

                return result;
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorOpt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<ErrorResponse>(rawError, errorOpt);
                    throw new InvalidOperationException(errorObj?.Error ?? "Invalid credentials.");
                }
                catch (System.Text.Json.JsonException)
                {
                    throw new InvalidOperationException($"Backend Error 500: {rawError}");
                }
            }
        }
        
        public async Task RefreshTokenAsync()
        {
            // Endpoint yêu cầu Header Authorization vì có [Authorize]
            var response = await _httpClient.PostAsync("api/auth/refresh-token", null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserAuthentication(result.Token);
                }
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).NotifyUserLogout();
        }
    }
}
