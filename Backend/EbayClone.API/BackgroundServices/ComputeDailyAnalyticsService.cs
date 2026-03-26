using System;
using System.Collections.Generic;
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
    /// [Phase 3B] Compute Daily Analytics — chạy 2:00 AM hàng đêm.
    /// Aggregate Orders ngày hôm trước → UPSERT ShopAnalyticsDaily.
    /// 
    /// Performance benefit:
    ///   - Dashboard SalesChart query 31 rows (ShopAnalyticsDaily) thay vì scan thousands Orders
    ///   - Pre-computed tại off-peak → zero computation cost cho user requests
    /// 
    /// Design:
    ///   - UPSERT pattern: nếu row đã tồn tại → UPDATE, else → INSERT
    ///   - Dùng raw SQL cho efficiency (EF không hỗ trợ MERGE natively)
    ///   - Chạy cho TẤT CẢ shops active — batch processing
    /// </summary>
    public class ComputeDailyAnalyticsService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ComputeDailyAnalyticsService> _logger;

        // Demo: chạy mỗi 6h. Production: 24h
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

        public ComputeDailyAnalyticsService(
            IServiceScopeFactory scopeFactory,
            ILogger<ComputeDailyAnalyticsService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ComputeDailyAnalyticsService started. Interval: {Interval}", CheckInterval);

            // Delay 20s — cho DB ổn định
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ComputeAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ComputeDailyAnalytics: Error during computation");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }

            _logger.LogInformation("ComputeDailyAnalyticsService stopped.");
        }

        private async Task ComputeAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            // [Performance Phase 2] Distributed Lock — chỉ 1 instance compute
            var lockService = scope.ServiceProvider.GetRequiredService<EbayClone.Application.Interfaces.IDistributedLockService>();
            if (!await lockService.TryAcquireLockAsync("compute-analytics", TimeSpan.FromHours(5), ct))
            {
                _logger.LogDebug("ComputeAnalytics: Another instance is processing. Skipping.");
                return;
            }

            try
            {
                var db = scope.ServiceProvider.GetRequiredService<EbayDbContext>();

                // Compute cho ngày hôm qua (hoặc hôm nay nếu chạy cuối ngày)
                var targetDate = DateTime.UtcNow.Date.AddDays(-1);

                _logger.LogInformation("ComputeDailyAnalytics: Computing for date {Date}...", targetDate);

                // Aggregate Orders cho mỗi shop vào ngày target
                // [FIX] SQL Server Error 130: không dùng nested aggregate Sum(Sum(...))
                // Tách thành 2 queries riêng biệt
                var orderStats = await db.Orders
                    .Where(o => o.PaidAt.HasValue &&
                                o.PaidAt.Value.Date == targetDate &&
                                o.Status != "CANCELLED")
                    .GroupBy(o => o.ShopId)
                    .Select(g => new
                    {
                        ShopId = g.Key,
                        TotalRevenue = g.Sum(o => o.TotalAmount),
                        TotalOrders = g.Count()
                    })
                    .ToListAsync(ct);

                // ItemsSold: flatten Order→Items rồi GroupBy ShopId
                // Dùng Select + SelectMany để carry ShopId từ parent Order
                var itemsSoldStats = await db.Orders
                    .Where(o => o.PaidAt.HasValue &&
                                o.PaidAt.Value.Date == targetDate &&
                                o.Status != "CANCELLED")
                    .SelectMany(o => o.Items.Select(i => new { o.ShopId, i.Quantity }))
                    .GroupBy(x => x.ShopId)
                    .Select(g => new
                    {
                        ShopId = g.Key,
                        ItemsSold = g.Sum(x => x.Quantity)
                    })
                    .ToListAsync(ct);

                var itemsSoldDict = itemsSoldStats.ToDictionary(x => x.ShopId, x => x.ItemsSold);

                var dailyStats = orderStats.Select(o => new
                {
                    o.ShopId,
                    o.TotalRevenue,
                    o.TotalOrders,
                    ItemsSold = itemsSoldDict.GetValueOrDefault(o.ShopId, 0)
                }).ToList();

                // Aggregate ProductViewLogs cho mỗi shop
                var dailyViews = await db.Set<Domain.Entities.ProductViewLog>()
                    .Where(v => v.ViewedAt.Date == targetDate)
                    .GroupBy(v => v.ShopId)
                    .Select(g => new
                    {
                        ShopId = g.Key,
                        ViewsCount = g.Count()
                    })
                    .ToListAsync(ct);

                var viewsDict = dailyViews.ToDictionary(v => v.ShopId, v => v.ViewsCount);

                // [Performance Phase 1] Batch UPSERT: Load tất cả existing records cho targetDate trong 1 query
                // Trước đây: N queries (1 per shop) → bây giờ: 1 query
                var allShopIds = dailyStats.Select(s => s.ShopId)
                    .Union(dailyViews.Select(v => v.ShopId))
                    .Distinct()
                    .ToList();

                var existingRecords = await db.Set<Domain.Entities.ShopAnalyticsDaily>()
                    .Where(x => x.ReportDate == targetDate && allShopIds.Contains(x.ShopId))
                    .ToDictionaryAsync(x => x.ShopId, ct);

                int upsertCount = 0;

                foreach (var stat in dailyStats)
                {
                    viewsDict.TryGetValue(stat.ShopId, out int views);

                    if (existingRecords.TryGetValue(stat.ShopId, out var existing))
                    {
                        // UPDATE existing record
                        existing.TotalRevenue = stat.TotalRevenue;
                        existing.TotalOrders = stat.TotalOrders;
                        existing.ItemsSold = stat.ItemsSold;
                        existing.ViewsCount = views;
                    }
                    else
                    {
                        // INSERT new record
                        db.Set<Domain.Entities.ShopAnalyticsDaily>().Add(new Domain.Entities.ShopAnalyticsDaily
                        {
                            ShopId = stat.ShopId,
                            ReportDate = targetDate,
                            TotalRevenue = stat.TotalRevenue,
                            TotalOrders = stat.TotalOrders,
                            ItemsSold = stat.ItemsSold,
                            ViewsCount = views
                        });
                    }
                    upsertCount++;
                }

                // Also insert view-only entries (shops with views but no orders)
                foreach (var view in dailyViews)
                {
                    if (dailyStats.Any(s => s.ShopId == view.ShopId)) continue;

                    if (existingRecords.TryGetValue(view.ShopId, out var existing))
                    {
                        existing.ViewsCount = view.ViewsCount;
                    }
                    else
                    {
                        db.Set<Domain.Entities.ShopAnalyticsDaily>().Add(new Domain.Entities.ShopAnalyticsDaily
                        {
                            ShopId = view.ShopId,
                            ReportDate = targetDate,
                            ViewsCount = view.ViewsCount
                        });
                    }
                    upsertCount++;
                }

                if (upsertCount > 0)
                {
                    await db.SaveChangesAsync(ct);
                }

                _logger.LogInformation(
                    "ComputeDailyAnalytics: Computed {Count} shop analytics for {Date}",
                    upsertCount, targetDate);
            }
            finally
            {
                await lockService.ReleaseLockAsync("compute-analytics", ct);
            }
        }
    }
}
