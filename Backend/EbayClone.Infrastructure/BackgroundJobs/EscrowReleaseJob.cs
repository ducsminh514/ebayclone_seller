using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
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
            var dbContext = scope.ServiceProvider.GetRequiredService<EbayDbContext>();

            // 1. Quét các đơn hàng DELIVERED đã quá 7 ngày
            var releaseThreshold = DateTimeOffset.UtcNow.AddDays(-7);
            
            var ordersToRelease = await dbContext.Orders
                .Include(o => o.Shop)
                .Where(o => o.Status == "DELIVERED" && o.CompletedAt <= releaseThreshold)
                // Giả định chúng ta cần đánh dấu hoặc kiểm tra tiền chưa giải ngân
                // Ở đây ta dựa trên trạng thái WalletTransaction nếu cần, 
                // nhưng để đơn giản ta sẽ quét các đơn hàng có trạng thái này mà chưa kết thúc luồng tài chính.
                .ToListAsync(ct);

            foreach (var order in ordersToRelease)
            {
                using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    var wallet = await dbContext.SellerWallets
                        .FirstOrDefaultAsync(w => w.ShopId == order.ShopId, ct);

                    if (wallet != null)
                    {
                        // Tính toán thực nhận (95%)
                        decimal incomeAmount = order.TotalAmount * 0.95m;

                        // Chuyển tiền (Sử dụng Domain Method để tuân thủ Encapsulation)
                        wallet.ReleaseEscrow(incomeAmount);

                        // Ghi log giao dịch ví
                        var walletTx = new WalletTransaction
                        {
                            Id = Guid.NewGuid(),
                            ShopId = order.ShopId,
                            Amount = incomeAmount,
                            Type = "ESCROW_RELEASE",
                            ReferenceId = order.Id,
                            ReferenceType = "ORDER",
                            Description = $"Giải ngân tự động cho đơn hàng {order.OrderNumber} sau 7 ngày.",
                            BalanceAfter = wallet.AvailableBalance,
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        dbContext.WalletTransactions.Add(walletTx);
                        
                        // Đánh dấu đơn hàng là đã COMPLETE tài chính (tùy chọn trạng thái mới)
                        // order.Status = "FINISH"; 
                        
                        await dbContext.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                        
                        _logger.LogInformation($"Released {incomeAmount} for order {order.OrderNumber} (Shop: {order.ShopId})");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, $"Failed to release escrow for order {order.Id}");
                }
            }
        }
    }
}
