using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace EbayClone.Frontend.Services
{
    /// <summary>
    /// DelegatingHandler tự động gắn Bearer token từ localStorage
    /// vào mỗi outgoing HTTP request. Giải quyết race condition khi
    /// AuthenticationStateProvider chưa kịp set DefaultRequestHeaders.
    /// 
    /// Security: Token chỉ được đọc tại runtime, không cache static.
    /// Performance: Đọc localStorage mỗi request (~0.1ms cho WASM).
    /// </summary>
    public class AuthTokenHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthTokenHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
