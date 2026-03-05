using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class SellerWalletRepository : ISellerWalletRepository
    {
        private readonly EbayDbContext _context;

        public SellerWalletRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<SellerWallet?> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _context.SellerWallets.FirstOrDefaultAsync(w => w.ShopId == shopId, cancellationToken);
        }

        public async Task AddAsync(SellerWallet wallet, CancellationToken cancellationToken = default)
        {
            await _context.SellerWallets.AddAsync(wallet, cancellationToken);
        }

        public void Update(SellerWallet wallet)
        {
            _context.SellerWallets.Update(wallet);
        }
    }
}
