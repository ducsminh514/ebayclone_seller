using System;

namespace EbayClone.Shared.DTOs.Policies
{
    public class PolicyCreationResponse
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
