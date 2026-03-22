using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class SellerDefectRepository : ISellerDefectRepository
    {
        private readonly EbayDbContext _context;

        public SellerDefectRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(SellerDefect defect, CancellationToken ct = default)
        {
            await _context.SellerDefects.AddAsync(defect, ct);
            // Không gọi SaveChangesAsync ở đây — để UnitOfWork quản lý transaction
        }

        public async Task<int> CountByShopInPeriodAsync(Guid shopId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        {
            return await _context.SellerDefects
                .Where(d => d.ShopId == shopId && d.CreatedAt >= from && d.CreatedAt <= to)
                .CountAsync(ct);
        }

        public async Task<int> CountByShopAndTypeAsync(Guid shopId, string defectType, CancellationToken ct = default)
        {
            return await _context.SellerDefects
                .Where(d => d.ShopId == shopId && d.DefectType == defectType)
                .CountAsync(ct);
        }

        public async Task<List<SellerDefect>> GetByShopIdPagedAsync(Guid shopId, int page, int pageSize, CancellationToken ct = default)
        {
            return await _context.SellerDefects
                .Where(d => d.ShopId == shopId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(d => d.Order)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<int> CountByShopAndTypeInPeriodAsync(Guid shopId, string defectType, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        {
            return await _context.SellerDefects
                .Where(d => d.ShopId == shopId && d.DefectType == defectType && d.CreatedAt >= from && d.CreatedAt <= to)
                .CountAsync(ct);
        }
    }
}
