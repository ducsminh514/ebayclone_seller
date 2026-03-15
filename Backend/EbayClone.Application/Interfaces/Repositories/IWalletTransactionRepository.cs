using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IWalletTransactionRepository
    {
        Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
        Task<List<WalletTransaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);
    }
}
