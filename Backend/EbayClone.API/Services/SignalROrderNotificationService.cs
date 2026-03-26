using EbayClone.API.Hubs;
using EbayClone.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EbayClone.API.Services
{
    /// <summary>
    /// SignalR implementation cho IOrderNotificationService.
    /// Dùng IHubContext (không inject Hub trực tiếp — thread-safe cho DI scoped).
    /// 
    /// [Scalability] Redis Backplane đảm bảo message broadcast tới tất cả instances.
    /// [Reliability] try-catch mỗi notification — lỗi SignalR không ảnh hưởng business logic.
    /// </summary>
    public class SignalROrderNotificationService : IOrderNotificationService
    {
        private readonly IHubContext<OrderHub> _hubContext;
        private readonly ILogger<SignalROrderNotificationService> _logger;

        public SignalROrderNotificationService(
            IHubContext<OrderHub> hubContext,
            ILogger<SignalROrderNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyNewOrderAsync(Guid shopId, Guid orderId, string orderNumber, decimal totalAmount)
        {
            try
            {
                await _hubContext.Clients.Group($"shop_{shopId}")
                    .SendAsync("NewOrder", new
                    {
                        OrderId = orderId,
                        OrderNumber = orderNumber,
                        TotalAmount = totalAmount,
                        Timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("SignalR: NewOrder pushed — Shop={ShopId}, Order={OrderNumber}", shopId, orderNumber);
            }
            catch (Exception ex)
            {
                // Notification failure MUST NOT break order creation
                _logger.LogWarning(ex, "SignalR: Failed to push NewOrder — Shop={ShopId}", shopId);
            }
        }

        public async Task NotifyOrderStatusChangedAsync(Guid shopId, Guid orderId, string orderNumber, string oldStatus, string newStatus)
        {
            try
            {
                await _hubContext.Clients.Group($"shop_{shopId}")
                    .SendAsync("OrderStatusChanged", new
                    {
                        OrderId = orderId,
                        OrderNumber = orderNumber,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        Timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("SignalR: StatusChanged pushed — Order={OrderNumber}, {Old}→{New}", 
                    orderNumber, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR: Failed to push StatusChanged — Shop={ShopId}", shopId);
            }
        }

        public async Task NotifyReturnRequestedAsync(Guid shopId, Guid orderId, string orderNumber)
        {
            try
            {
                await _hubContext.Clients.Group($"shop_{shopId}")
                    .SendAsync("ReturnRequested", new
                    {
                        OrderId = orderId,
                        OrderNumber = orderNumber,
                        Timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("SignalR: ReturnRequested pushed — Order={OrderNumber}", orderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR: Failed to push ReturnRequested — Shop={ShopId}", shopId);
            }
        }

        public async Task NotifyDisputeOpenedAsync(Guid shopId, Guid orderId, string orderNumber, string disputeType)
        {
            try
            {
                await _hubContext.Clients.Group($"shop_{shopId}")
                    .SendAsync("DisputeOpened", new
                    {
                        OrderId = orderId,
                        OrderNumber = orderNumber,
                        DisputeType = disputeType,
                        Timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("SignalR: DisputeOpened pushed — Order={OrderNumber}, Type={Type}", 
                    orderNumber, disputeType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR: Failed to push DisputeOpened — Shop={ShopId}", shopId);
            }
        }
    }
}
