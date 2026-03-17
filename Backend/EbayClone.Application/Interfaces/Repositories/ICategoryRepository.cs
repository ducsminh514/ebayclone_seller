using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
        
        // [A5] Item Specifics per category
        Task<IEnumerable<CategoryItemSpecific>> GetItemSpecificsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);
        
        // [A7] Seed support
        Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default);
        Task AddItemSpecificsRangeAsync(IEnumerable<CategoryItemSpecific> specifics, CancellationToken cancellationToken = default);
    }
}
