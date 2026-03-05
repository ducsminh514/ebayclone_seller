using System;

namespace EbayClone.Application.DTOs.Policies
{
    public class ShippingPolicyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int HandlingTimeDays { get; set; }
        public decimal Cost { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ReturnPolicyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ReturnDays { get; set; }
        public string ShippingPaidBy { get; set; } = string.Empty;
    }
}
