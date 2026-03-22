using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EbayClone.Application.UseCases.Analytics
{
    public interface IEvaluateSellerLevelUseCase
    {
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// [Phase 2] Batch evaluate Seller Level cho tất cả shops.
    /// 
    /// Rules (theo eBay thật):
    ///   - Shop < 90 ngày → giữ NEW
    ///   - DefectRate > 2% → BELOW_STANDARD
    ///   - DefectRate ≤ 0.5% AND LateRate ≤ 3% AND Txns ≥ 100 → TOP_RATED
    ///   - Else → ABOVE_STANDARD
    /// 
    /// Performance: Batch process all shops, dùng composite index (ShopId, CreatedAt).
    /// Scalability: Với 1000 shops, mỗi shop 3 COUNT queries → ~3000 queries.
    ///   Nếu scale lớn hơn → cần pre-aggregate (Phase 3).
    /// </summary>
    public class EvaluateSellerLevelUseCase : IEvaluateSellerLevelUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly ISellerDefectRepository _defectRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EvaluateSellerLevelUseCase> _logger;

        public EvaluateSellerLevelUseCase(
            IShopRepository shopRepository,
            ISellerDefectRepository defectRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork,
            ILogger<EvaluateSellerLevelUseCase> logger)
        {
            _shopRepository = shopRepository;
            _defectRepository = defectRepository;
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var shops = await _shopRepository.GetAllActiveShopsAsync(cancellationToken);
            int updatedCount = 0;
            var now = DateTimeOffset.UtcNow;

            foreach (var shop in shops)
            {
                try
                {
                    var oldLevel = shop.SellerLevel;
                    var newLevel = await EvaluateShopAsync(shop, now, cancellationToken);

                    if (oldLevel != newLevel)
                    {
                        shop.SellerLevel = newLevel;
                        shop.LevelEvaluatedAt = now;
                        _shopRepository.Update(shop);
                        updatedCount++;

                        _logger.LogInformation(
                            "Seller Level changed: Shop {ShopId} ({ShopName}): {Old} → {New}",
                            shop.Id, shop.Name, oldLevel, newLevel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating seller level for Shop {ShopId}", shop.Id);
                    // Tiếp tục với shop khác — không fail batch
                }
            }

            // Persist tất cả changes 1 lần (batch save, không per-shop)
            if (updatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return updatedCount;
        }

        private async Task<string> EvaluateShopAsync(Shop shop, DateTimeOffset now, CancellationToken ct)
        {
            // Rule 1: Shop < 90 ngày → giữ NEW
            var shopAge = now - shop.CreatedAt;
            if (shopAge.TotalDays < 90)
                return SellerLevels.NEW;

            // Xác định evaluation period
            // eBay: ≥400 txns trong 3 tháng → dùng 3 tháng, else 12 tháng
            var period3m = now.AddMonths(-3);
            var period12m = now.AddMonths(-12);

            var txns3m = await _orderRepository.CountCompletedInPeriodAsync(shop.Id, period3m, now, ct);
            
            DateTimeOffset periodStart;
            int transactionsInPeriod;
            
            if (txns3m >= 400)
            {
                periodStart = period3m;
                transactionsInPeriod = txns3m;
            }
            else
            {
                periodStart = period12m;
                transactionsInPeriod = await _orderRepository.CountCompletedInPeriodAsync(shop.Id, period12m, now, ct);
            }

            // Chưa đủ data → ABOVE_STANDARD (default tốt hơn BELOW_STANDARD)
            if (transactionsInPeriod == 0)
                return SellerLevels.ABOVE_STANDARD;

            // Count defects + late shipments trong evaluation period
            var defectsTask = _defectRepository.CountByShopInPeriodAsync(shop.Id, periodStart, now, ct);
            var lateTask = _defectRepository.CountByShopAndTypeInPeriodAsync(
                shop.Id, DefectTypes.LATE_SHIPMENT, periodStart, now, ct);

            await Task.WhenAll(defectsTask, lateTask);

            var defectsInPeriod = await defectsTask;
            var lateInPeriod = await lateTask;

            decimal defectRate = (decimal)defectsInPeriod / transactionsInPeriod * 100;
            decimal lateRate = (decimal)lateInPeriod / transactionsInPeriod * 100;

            // Rule 2: Defect > 2% → BELOW_STANDARD
            if (defectRate > 2.0m)
                return SellerLevels.BELOW_STANDARD;

            // Rule 3: TOP_RATED requirements
            // DefectRate ≤ 0.5% AND LateRate ≤ 3% AND ≥100 transactions
            if (defectRate <= 0.5m && lateRate <= 3.0m && transactionsInPeriod >= 100)
                return SellerLevels.TOP_RATED;

            // Rule 4: Default
            return SellerLevels.ABOVE_STANDARD;
        }
    }
}
