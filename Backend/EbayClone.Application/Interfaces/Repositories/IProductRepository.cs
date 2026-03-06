using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task AddAsync(Product product, CancellationToken cancellationToken = default);
        Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetProductsByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
        
        Task AddVariantsAsync(IEnumerable<ProductVariant> variants, CancellationToken cancellationToken = default);
        Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
        
        // Optimistic Concurrency Update for Restock
        Task<int> RestockVariantAsync(Guid variantId, int addedQuantity, CancellationToken cancellationToken = default);
        
        // Fulfillment Atomics
        Task<int> DeductStockAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
        Task<int> ReleaseReservationAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
        Task<int> ReserveStockAtomicAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
        
        // Kiểm tra giới hạn MonthlyListingLimit
        Task<int> CountProductsThisMonthAsync(Guid shopId, CancellationToken cancellationToken = default);
    }
}
