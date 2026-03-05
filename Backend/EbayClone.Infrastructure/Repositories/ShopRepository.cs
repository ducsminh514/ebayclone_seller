using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EbayClone.Infrastructure.Repositories
{
    public class ShopRepository : IShopRepository
    {
        private readonly EbayDbContext _context;

        public ShopRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Shops.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<Shop?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.OwnerId == userId, cancellationToken);
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Shops.AnyAsync(s => s.OwnerId == userId, cancellationToken);
        }

        public async Task AddAsync(Shop shop, CancellationToken cancellationToken = default)
        {
            await _context.Shops.AddAsync(shop, cancellationToken);
        }

        public void Update(Shop shop)
        {
            _context.Shops.Update(shop);
        }
    }
}
