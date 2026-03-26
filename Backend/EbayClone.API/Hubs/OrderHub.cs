using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EbayClone.API.Hubs
{
    /// <summary>
    /// SignalR Hub cho real-time order notifications.
    /// 
    /// [Security] JWT Authenticated — chỉ seller đã login mới connect được.
    /// [Scalability] Group by ShopId — mỗi seller chỉ nhận events của shop mình.
    /// [Performance] One-way push — client không gọi method lên server (giảm attack surface).
    /// </summary>
    [Authorize]
    public class OrderHub : Hub
    {
        private readonly ILogger<OrderHub> _logger;

        public OrderHub(ILogger<OrderHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // Lấy ShopId từ JWT claims — KHÔNG trust client input (anti-spoofing)
            var shopIdClaim = Context.User?.FindFirst("ShopId")?.Value;
            
            if (!string.IsNullOrEmpty(shopIdClaim))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shopIdClaim}");
                _logger.LogInformation("SignalR: Seller connected — ShopId={ShopId}, ConnectionId={ConnectionId}", 
                    shopIdClaim, Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("SignalR: Connected user has no ShopId claim — ConnectionId={ConnectionId}", 
                    Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var shopIdClaim = Context.User?.FindFirst("ShopId")?.Value;
            
            if (!string.IsNullOrEmpty(shopIdClaim))
            {
                _logger.LogInformation("SignalR: Seller disconnected — ShopId={ShopId}", shopIdClaim);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
