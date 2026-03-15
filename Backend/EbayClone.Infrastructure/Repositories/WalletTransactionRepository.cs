using Microsoft.EntityFrameworkCore;
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
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
        }
    }
}
