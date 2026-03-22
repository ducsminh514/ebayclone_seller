using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface ISellerDefectRepository
    {
        Task AddAsync(SellerDefect defect, CancellationToken ct = default);
        Task<int> CountByShopInPeriodAsync(Guid shopId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
        Task<int> CountByShopAndTypeAsync(Guid shopId, string defectType, CancellationToken ct = default);
        Task<List<SellerDefect>> GetByShopIdPagedAsync(Guid shopId, int page, int pageSize, CancellationToken ct = default);
        Task<int> CountByShopAndTypeInPeriodAsync(Guid shopId, string defectType, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    }
}
