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
    }
}
