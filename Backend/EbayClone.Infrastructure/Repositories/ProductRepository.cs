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
    public class ProductRepository : IProductRepository
    {
        private readonly EbayDbContext _context;

        public ProductRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            await _context.Products.AddAsync(product, cancellationToken);
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            _context.Products.Update(product);
            return Task.CompletedTask;
        }

        public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                                 .Include(p => p.Variants)
                                 .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetProductsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            // Fix N+1 Query Using AsSplitQuery
            return await _context.Products
                                 .AsNoTracking()
                                 .Where(p => p.ShopId == shopId)
                                 .Include(p => p.Variants)
                                 .AsSplitQuery()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountByShopInCurrentMonthAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            return await _context.Products
                .CountAsync(p => p.ShopId == shopId && p.CreatedAt >= firstDayOfMonth, cancellationToken);
        }

        public async Task AddVariantsAsync(IEnumerable<ProductVariant> variants, CancellationToken cancellationToken = default)
        {
            await _context.ProductVariants.AddRangeAsync(variants, cancellationToken);
        }

        public async Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
        {
            return await _context.ProductVariants.FindAsync(new object[] { variantId }, cancellationToken);
        }

        public async Task<int> RestockVariantAsync(Guid variantId, int addedQuantity, CancellationToken cancellationToken = default)
        {
            // Đẩy lệnh trực tiếp vào CSDL bằng ExecuteUpdateAsync chống Race Condition (TOCTOU)
            return await _context.ProductVariants
                .Where(v => v.Id == variantId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Quantity, v => v.Quantity + addedQuantity), cancellationToken);
        }

        public async Task<int> DeductStockAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        {
            return await _context.ProductVariants
                .Where(v => v.Id == variantId && v.Quantity >= quantity && v.ReservedQuantity >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Quantity, x => x.Quantity - quantity)
                    .SetProperty(x => x.ReservedQuantity, x => x.ReservedQuantity - quantity), 
                    cancellationToken);
        }

        public async Task<int> ReleaseReservationAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        {
            return await _context.ProductVariants
                .Where(v => v.Id == variantId && v.ReservedQuantity >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.ReservedQuantity, x => x.ReservedQuantity - quantity), 
                    cancellationToken);
        }

        public async Task<int> ReserveStockAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
        {
            // AvailableStock = Quantity - ReservedQuantity
            // Condition to Reserve: Quantity - ReservedQuantity >= quantity
            return await _context.ProductVariants
                .Where(v => v.Id == variantId && (v.Quantity - v.ReservedQuantity) >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.ReservedQuantity, x => x.ReservedQuantity + quantity), 
                    cancellationToken);
        }
    }
}
