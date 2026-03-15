using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EbayClone.API.BackgroundServices
{
    /// <summary>
    /// Background Service chạy mỗi 1 phút.
    /// Nhiệm vụ: Quét các sản phẩm có Status = "SCHEDULED" và ScheduledAt <= UTC now
    /// → Tự động đổi sang "ACTIVE" để xuất hiện với buyer đúng giờ.
    ///
    /// Design note:
    /// - Dùng IHostedService (built-in .NET) thay vì Hangfire để không thêm dependency nặng
    /// - Scoped service (EbayDbContext) phải lấy qua IServiceScopeFactory vì BackgroundService là singleton
    /// - ExecuteUpdateAsync thay vì fetch-then-update để tránh race condition khi scale nhiều server
    /// </summary>
    public class ScheduledListingActivatorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledListingActivatorService> _logger;

        // Chạy mỗi 1 phút - đủ chính xác, không tốn tài nguyên
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public ScheduledListingActivatorService(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduledListingActivatorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledListingActivatorService started. Check interval: {Interval}", CheckInterval);

            // Delay 10 giây sau khi app start để DB connection ổn định
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ActivateScheduledListingsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // App đang shutdown, thoát gracefully
                    break;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không crash service - vòng lặp tiếp tục ở lần sau
                    _logger.LogError(ex, "Error occurred while activating scheduled listings.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("ScheduledListingActivatorService stopped.");
        }

        private async Task ActivateScheduledListingsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EbayDbContext>();

            var nowUtc = DateTimeOffset.UtcNow;

            // Atomic UPDATE trực tiếp xuống SQL - không cần fetch vào RAM
            // WHERE Status = 'SCHEDULED' AND ScheduledAt <= NOW AND IsDeleted = 0
            // → SET Status = 'ACTIVE', UpdatedAt = NOW
            var updatedCount = await dbContext.Products
                .Where(p =>
                    p.Status == "SCHEDULED" &&
                    p.ScheduledAt.HasValue &&
                    p.ScheduledAt.Value <= nowUtc &&
                    !p.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Status, "ACTIVE")
                    .SetProperty(p => p.UpdatedAt, nowUtc),
                cancellationToken);

            if (updatedCount > 0)
            {
                _logger.LogInformation(
                    "ScheduledListingActivator: Activated {Count} product(s) at {Time}",
                    updatedCount, nowUtc);
            }
        }
    }
}
