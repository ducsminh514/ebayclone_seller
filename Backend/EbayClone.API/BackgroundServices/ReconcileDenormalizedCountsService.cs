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
    /// [Phase 3A] Reconciliation Job — chạy 3:00 AM hàng đêm.
    /// Fix denormalized count drift do CheckAndUpdateStockStatus() bypass UseCase.
    /// 
    /// Strategy:
    ///   1. Recalculate ActiveListingCount, DraftListingCount, AwaitingShipmentCount từ actual data
    ///   2. Log delta để phát hiện drift patterns
    ///   3. Dùng ExecuteUpdateAsync + subquery — KHÔNG load entities vào RAM
    /// 
    /// Performance note:
    ///   - Chạy off-peak hours (3AM) để tránh lock contention
    ///   - 3 atomic UPDATE statements — O(1) per shop, O(N) total
    ///   - Không ảnh hưởng user-facing requests
    /// </summary>
    public class ReconcileDenormalizedCountsService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReconcileDenormalizedCountsService> _logger;

        // Chạy mỗi 24h. Demo mode: mỗi 6h để thấy effect nhanh hơn.
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        public ReconcileDenormalizedCountsService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReconcileDenormalizedCountsService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReconcileDenormalizedCountsService started. Interval: {Interval}", CheckInterval);

            // Delay 30s sau khi app start — cho các service khác ổn định
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ReconcileAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ReconcileDenormalizedCounts: Error during reconciliation");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("ReconcileDenormalizedCountsService stopped.");
        }

        private async Task ReconcileAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EbayDbContext>();

            _logger.LogInformation("ReconcileDenormalizedCounts: Starting reconciliation...");

            // Bước 1: Snapshot current values để log drift (optional — chỉ cho monitoring)
            var shopsWithDrift = await db.Shops
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    Current_Active = s.ActiveListingCount,
                    Current_Draft = s.DraftListingCount,
                    Current_Awaiting = s.AwaitingShipmentCount,
                    Actual_Active = db.Products.Count(p => p.ShopId == s.Id && p.Status == "ACTIVE" && !p.IsDeleted),
                    Actual_Draft = db.Products.Count(p => p.ShopId == s.Id && p.Status == "DRAFT" && !p.IsDeleted),
                    Actual_Awaiting = db.Orders.Count(o => o.ShopId == s.Id && o.Status == "PAID")
                })
                .Where(x =>
                    x.Current_Active != x.Actual_Active ||
                    x.Current_Draft != x.Actual_Draft ||
                    x.Current_Awaiting != x.Actual_Awaiting)
                .ToListAsync(ct);

            if (shopsWithDrift.Count > 0)
            {
                foreach (var drift in shopsWithDrift)
                {
                    _logger.LogWarning(
                        "ReconcileDenormalizedCounts: DRIFT detected for Shop '{ShopName}' (Id={ShopId}): " +
                        "Active {OldActive}→{NewActive}, Draft {OldDraft}→{NewDraft}, Awaiting {OldAwaiting}→{NewAwaiting}",
                        drift.Name, drift.Id,
                        drift.Current_Active, drift.Actual_Active,
                        drift.Current_Draft, drift.Actual_Draft,
                        drift.Current_Awaiting, drift.Actual_Awaiting);
                }
            }

            // Bước 2: Atomic UPDATE tất cả shops cùng lúc — dùng correlated subquery
            // ActiveListingCount
            var activeFixed = await db.Shops
                .Where(s => s.ActiveListingCount != db.Products.Count(p => p.ShopId == s.Id && p.Status == "ACTIVE" && !p.IsDeleted))
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(s => s.ActiveListingCount,
                        s => db.Products.Count(p => p.ShopId == s.Id && p.Status == "ACTIVE" && !p.IsDeleted)),
                ct);

            // DraftListingCount
            var draftFixed = await db.Shops
                .Where(s => s.DraftListingCount != db.Products.Count(p => p.ShopId == s.Id && p.Status == "DRAFT" && !p.IsDeleted))
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(s => s.DraftListingCount,
                        s => db.Products.Count(p => p.ShopId == s.Id && p.Status == "DRAFT" && !p.IsDeleted)),
                ct);

            // AwaitingShipmentCount
            var awaitingFixed = await db.Shops
                .Where(s => s.AwaitingShipmentCount != db.Orders.Count(o => o.ShopId == s.Id && o.Status == "PAID"))
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(s => s.AwaitingShipmentCount,
                        s => db.Orders.Count(o => o.ShopId == s.Id && o.Status == "PAID")),
                ct);

            var totalFixed = activeFixed + draftFixed + awaitingFixed;
            if (totalFixed > 0)
            {
                _logger.LogInformation(
                    "ReconcileDenormalizedCounts: Fixed {Total} shop(s) — Active:{Active}, Draft:{Draft}, Awaiting:{Awaiting}",
                    totalFixed, activeFixed, draftFixed, awaitingFixed);
            }
            else
            {
                _logger.LogInformation("ReconcileDenormalizedCounts: All counts consistent. No fixes needed.");
            }
        }
    }
}
