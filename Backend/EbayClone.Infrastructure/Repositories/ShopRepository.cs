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

        public async Task IncrementTotalShippingPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalShippingPolicies, x => x.TotalShippingPolicies + 1), cancellationToken);
        }

        public async Task IncrementTotalReturnPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalReturnPolicies, x => x.TotalReturnPolicies + 1), cancellationToken);
        }

        public async Task IncrementTotalPaymentPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalPaymentPolicies, x => x.TotalPaymentPolicies + 1), cancellationToken);
        }

        // Atomic Decrement (đảm bảo >= 0)
        public async Task DecrementTotalShippingPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id && s.TotalShippingPolicies > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalShippingPolicies, x => x.TotalShippingPolicies - 1), cancellationToken);
        }

        public async Task DecrementTotalReturnPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id && s.TotalReturnPolicies > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalReturnPolicies, x => x.TotalReturnPolicies - 1), cancellationToken);
        }

        public async Task DecrementTotalPaymentPoliciesAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _context.Shops
                .Where(s => s.Id == id && s.TotalPaymentPolicies > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalPaymentPolicies, x => x.TotalPaymentPolicies - 1), cancellationToken);
        }
    }
}
