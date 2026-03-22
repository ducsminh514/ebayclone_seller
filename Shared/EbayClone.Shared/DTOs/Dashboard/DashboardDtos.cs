using System;
using System.Collections.Generic;

namespace EbayClone.Shared.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        // ── Listing Counts ──
        public int ActiveCount { get; set; }
        public int DraftCount { get; set; }
        public int OrderCount { get; set; }
        public int UnsoldCount { get; set; }
        public decimal TotalSales90Days { get; set; }
        
        // Promotion / Limit stats
        public int MonthlyListingLimit { get; set; }
        public int UsedListingLimit { get; set; }
        public int RemainingListingLimit => Math.Max(0, MonthlyListingLimit - UsedListingLimit);

        // ── Seller Performance Metrics ──
        public string SellerLevel { get; set; } = "NEW";
        public decimal DefectRate { get; set; }
        public int DefectCount { get; set; }
        public int TotalTransactions { get; set; }
        public int LateShipmentCount { get; set; }
        public int FeedbackScore { get; set; }
        public decimal PositivePercent { get; set; }

        // ── Sales Chart (31-day trend) ──
        public List<DailySalesPoint> SalesChart { get; set; } = new();
    }

    public class DailySalesPoint
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
