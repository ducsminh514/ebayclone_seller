using System;

namespace EbayClone.Shared.DTOs.Dashboard
{
    public class DashboardStatsDto
    {
        public int ActiveCount { get; set; }
        public int DraftCount { get; set; }
        public int OrderCount { get; set; }
        public int UnsoldCount { get; set; }
        public decimal TotalSales90Days { get; set; }
        
        // Promotion / Limit stats
        public int MonthlyListingLimit { get; set; }
        public int UsedListingLimit { get; set; }
        public int RemainingListingLimit => Math.Max(0, MonthlyListingLimit - UsedListingLimit);
    }
}
