using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;

namespace EbayClone.Infrastructure.Repositories
{
    public class WalletTransactionRepository : IWalletTransactionRepository
    {
        private readonly EbayDbContext _context;

        public WalletTransactionRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.WalletTransactions.AddAsync(transaction, cancellationToken);
        }

        public async Task<List<WalletTransaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default)
        {
            return await _context.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<WalletTransaction> Items, int Total)> GetPagedAsync(
            Guid shopId,
            int page,
            int pageSize,
            string? type = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.WalletTransactions
                .AsNoTracking()
                .Where(t => t.ShopId == shopId);

            // Filter theo type (ORDER_INCOME, ESCROW_RELEASE, REFUND, PLATFORM_FEE, WITHDRAW, DISPUTE_HOLD)
            if (!string.IsNullOrEmpty(type))
                query = query.Where(t => t.Type == type);

            // Filter theo khoảng ngày
            if (from.HasValue)
                query = query.Where(t => t.CreatedAt >= from.Value);
            if (to.HasValue)
                query = query.Where(t => t.CreatedAt <= to.Value);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }
    }
}
