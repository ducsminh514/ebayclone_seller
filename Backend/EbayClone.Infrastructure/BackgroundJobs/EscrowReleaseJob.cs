using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using EbayClone.Application.UseCases.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EbayClone.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Job tự động giải ngân tiền (Escrow Release) sau 7 ngày kể từ khi DELIVERED.
    /// Theo đúng tiêu chuẩn eBay.
    /// </summary>
    public class EscrowReleaseJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EscrowReleaseJob> _logger;

        public EscrowReleaseJob(IServiceProvider serviceProvider, ILogger<EscrowReleaseJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Escrow Release Job is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessEscrowReleases(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Escrow Release Job.");
                }

                // Chạy định kỳ một lần mỗi ngày (hoặc cấu hình tùy ý)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("Escrow Release Job is stopping.");
        }

        private async Task ProcessEscrowReleases(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var releaseFundsUseCase = scope.ServiceProvider.GetRequiredService<IReleaseFundsUseCase>();
            
            _logger.LogInformation("Triggering unified ReleaseFundsUseCase from Background Job.");
            int count = await releaseFundsUseCase.ExecuteAsync(ct);
            
            if (count > 0)
            {
                _logger.LogInformation($"Successfully released funds for {count} orders via Background Job.");
            }
        }
    }
}
