using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface ISellerWalletRepository
    {
        Task<SellerWallet?> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        Task AddAsync(SellerWallet wallet, CancellationToken cancellationToken = default);
        void Update(SellerWallet wallet);
    }
}
