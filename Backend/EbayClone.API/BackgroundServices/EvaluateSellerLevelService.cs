using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.UseCases.Analytics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EbayClone.API.BackgroundServices
{
    /// <summary>
    /// Background Service đánh giá Seller Level tự động.
    /// 
    /// Schedule: Chạy mỗi 24 giờ, evaluate vào ngày 20 mỗi tháng (theo eBay thật).
    /// 
    /// Design note:
    /// - Dùng IServiceScopeFactory vì BackgroundService là singleton, DbContext là scoped.
    /// - Tách logic vào IEvaluateSellerLevelUseCase để testable + reusable.
    /// - Không fail toàn bộ nếu 1 shop lỗi — continue với shop khác.
    /// </summary>
    public class EvaluateSellerLevelService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EvaluateSellerLevelService> _logger;

        // Check mỗi 24 giờ
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        public EvaluateSellerLevelService(
            IServiceScopeFactory scopeFactory,
            ILogger<EvaluateSellerLevelService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EvaluateSellerLevelService started. Check interval: {Interval}", CheckInterval);

            // Delay 30 giây sau khi app start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // eBay evaluate ngày 20 mỗi tháng
                    // Demo mode: evaluate mỗi ngày để dễ test
                    var now = DateTime.UtcNow;
                    
                    // Production: if (now.Day == 20) { ... }
                    // Demo: chạy mỗi ngày
                    await EvaluateAllAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in EvaluateSellerLevelService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("EvaluateSellerLevelService stopped.");
        }

        private async Task EvaluateAllAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            // [Performance Phase 2] Distributed Lock — chỉ 1 instance evaluate
            var lockService = scope.ServiceProvider.GetRequiredService<EbayClone.Application.Interfaces.IDistributedLockService>();
            if (!await lockService.TryAcquireLockAsync("evaluate-seller-level", TimeSpan.FromHours(23), cancellationToken))
            {
                _logger.LogDebug("EvaluateSellerLevel: Another instance is processing. Skipping.");
                return;
            }

            try
            {
                var useCase = scope.ServiceProvider.GetRequiredService<IEvaluateSellerLevelUseCase>();
                var updatedCount = await useCase.ExecuteAsync(cancellationToken);

                _logger.LogInformation(
                    "EvaluateSellerLevelService: Evaluated all shops. {Count} level(s) changed at {Time}",
                    updatedCount, DateTimeOffset.UtcNow);
            }
            finally
            {
                await lockService.ReleaseLockAsync("evaluate-seller-level", cancellationToken);
            }
        }
    }
}
