using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace EbayClone.Frontend.Services
{
    /// <summary>
    /// Service quản lý kết nối SignalR tới OrderHub.
    /// 
    /// [Reliability] Auto-reconnect với exponential backoff (0s → 2s → 5s → 10s → 30s).
    /// [Security] Truyền JWT qua query string (WebSocket không hỗ trợ Authorization header).
    /// [Performance] Reuse single HubConnection per scope — không tạo nhiều connections.
    /// </summary>
    public class OrderHubService : IAsyncDisposable
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IConfiguration _configuration;
        private HubConnection? _hubConnection;

        // Events cho UI subscribe
        public event Action<Guid, string, decimal>? OnNewOrder;           // orderId, orderNumber, totalAmount
        public event Action<Guid, string, string, string>? OnOrderStatusChanged;  // orderId, orderNumber, oldStatus, newStatus
        public event Action<Guid, string>? OnReturnRequested;             // orderId, orderNumber
        public event Action<Guid, string, string>? OnDisputeOpened;       // orderId, orderNumber, disputeType

        // Connection state events
        public event Action<string>? OnConnectionStateChanged;  // "Connected" | "Reconnecting" | "Reconnected" | "Disconnected"

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public OrderHubService(ILocalStorageService localStorage, IConfiguration configuration)
        {
            _localStorage = localStorage;
            _configuration = configuration;
        }

        public async Task StartAsync()
        {
            if (_hubConnection != null) return;

            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token)) return; // Chưa login → skip

            var apiBase = _configuration["ApiBaseUrl"] ?? "https://localhost:7250";
            var hubUrl = $"{apiBase}/hubs/orders?access_token={token}";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero,                 // Retry ngay lập tức
                    TimeSpan.FromSeconds(2),       // +2s
                    TimeSpan.FromSeconds(5),       // +5s
                    TimeSpan.FromSeconds(10),      // +10s
                    TimeSpan.FromSeconds(30)        // +30s (max)
                })
                .Build();

            // Register hub event handlers
            _hubConnection.On<OrderNotification>("NewOrder", notification =>
            {
                OnNewOrder?.Invoke(notification.OrderId, notification.OrderNumber, notification.TotalAmount);
            });

            _hubConnection.On<StatusChangeNotification>("OrderStatusChanged", notification =>
            {
                OnOrderStatusChanged?.Invoke(notification.OrderId, notification.OrderNumber, 
                    notification.OldStatus, notification.NewStatus);
            });

            _hubConnection.On<ReturnNotification>("ReturnRequested", notification =>
            {
                OnReturnRequested?.Invoke(notification.OrderId, notification.OrderNumber);
            });

            _hubConnection.On<DisputeNotification>("DisputeOpened", notification =>
            {
                OnDisputeOpened?.Invoke(notification.OrderId, notification.OrderNumber, notification.DisputeType);
            });

            // Connection lifecycle events
            _hubConnection.Reconnecting += ex =>
            {
                OnConnectionStateChanged?.Invoke("Reconnecting");
                Console.WriteLine($"[SignalR] Đang kết nối lại... {ex?.Message}");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                OnConnectionStateChanged?.Invoke("Reconnected");
                Console.WriteLine($"[SignalR] Đã kết nối lại. ConnectionId={connectionId}");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async ex =>
            {
                OnConnectionStateChanged?.Invoke("Disconnected");
                Console.WriteLine($"[SignalR] Mất kết nối. {ex?.Message}");
                
                // Manual retry sau khi hết auto-reconnect attempts
                await Task.Delay(TimeSpan.FromSeconds(30));
                try
                {
                    await _hubConnection.StartAsync();
                    OnConnectionStateChanged?.Invoke("Connected");
                }
                catch (Exception retryEx)
                {
                    Console.WriteLine($"[SignalR] Retry thất bại: {retryEx.Message}");
                }
            };

            try
            {
                await _hubConnection.StartAsync();
                OnConnectionStateChanged?.Invoke("Connected");
                Console.WriteLine("[SignalR] Connected to OrderHub");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SignalR] Connection failed: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        // DTOs cho deserialize SignalR messages
        private record OrderNotification(Guid OrderId, string OrderNumber, decimal TotalAmount, DateTimeOffset Timestamp);
        private record StatusChangeNotification(Guid OrderId, string OrderNumber, string OldStatus, string NewStatus, DateTimeOffset Timestamp);
        private record ReturnNotification(Guid OrderId, string OrderNumber, DateTimeOffset Timestamp);
        private record DisputeNotification(Guid OrderId, string OrderNumber, string DisputeType, DateTimeOffset Timestamp);
    }
}
