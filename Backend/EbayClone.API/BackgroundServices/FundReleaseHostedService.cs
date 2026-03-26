using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.UseCases.Orders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EbayClone.API.BackgroundServices
{
    /// <summary>
    /// Background Service tự động giải ngân (Escrow Release) theo SellerLevel.
    /// Chạy mỗi 5 phút để quét các đơn DELIVERED đã hết hold period → chuyển Pending → Available.
    ///
    /// Hold period theo SellerLevel:
    ///   NEW            = 21 ngày sau DELIVERED
    ///   BELOW_STANDARD = 14 ngày sau DELIVERED
    ///   ABOVE_STANDARD =  3 ngày sau DELIVERED
    ///   TOP_RATED      =  0 ngày (giải ngân ngay sau DELIVERED)
    ///
    /// Design note:
    /// - Dùng IServiceScopeFactory (bắt buộc) vì BackgroundService là singleton
    ///   còn EbayDbContext là scoped — không inject trực tiếp được.
    /// - Gọi IReleaseFundsUseCase.ExecuteAsync() để tái dùng toàn bộ logic wallet.
    /// </summary>
    public class FundReleaseHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FundReleaseHostedService> _logger;

        // Kiểm tra mỗi 5 phút — cân bằng giữa độ trễ và tải DB
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        public FundReleaseHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<FundReleaseHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FundReleaseHostedService started. Check interval: {Interval}", CheckInterval);

            // Delay 15 giây sau khi app start để DB connection ổn định
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ReleaseFundsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // App đang shutdown, thoát gracefully
                    break;
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không crash service — vòng lặp tiếp tục ở lần sau
                    _logger.LogError(ex, "Error occurred in FundReleaseHostedService.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("FundReleaseHostedService stopped.");
        }

        private async Task ReleaseFundsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            // [Performance Phase 2] Distributed Lock — chỉ 1 instance xử lý tại 1 thời điểm
            var lockService = scope.ServiceProvider.GetRequiredService<EbayClone.Application.Interfaces.IDistributedLockService>();
            if (!await lockService.TryAcquireLockAsync("fund-release", TimeSpan.FromMinutes(4), cancellationToken))
            {
                _logger.LogDebug("FundRelease: Another instance is processing. Skipping.");
                return;
            }

            try
            {
                var useCase = scope.ServiceProvider.GetRequiredService<IReleaseFundsUseCase>();
                var releasedCount = await useCase.ExecuteAsync(cancellationToken);

                if (releasedCount > 0)
                {
                    _logger.LogInformation(
                        "FundReleaseHostedService: Released funds for {Count} order(s) at {Time}",
                        releasedCount, DateTimeOffset.UtcNow);
                }
            }
            finally
            {
                await lockService.ReleaseLockAsync("fund-release", cancellationToken);
            }
        }
    }
}
