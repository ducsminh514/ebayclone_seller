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
    public class CategoryRepository : ICategoryRepository
    {
        private readonly EbayDbContext _context;

        public CategoryRepository(EbayDbContext context)
        {
            _context = context;
        }

        public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .Include(c => c.ItemSpecifics)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(c => c.SubCategories)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Category>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.ParentId == parentId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CategoryItemSpecific>> GetItemSpecificsByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return await _context.CategoryItemSpecifics
                .AsNoTracking()
                .Where(s => s.CategoryId == categoryId)
                .OrderBy(s => s.SortOrder)
                .ToListAsync(cancellationToken);
        }

        // [A7] Seed support
        public async Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default)
        {
            await _context.Categories.AddRangeAsync(categories, cancellationToken);
        }

        public async Task AddItemSpecificsRangeAsync(IEnumerable<CategoryItemSpecific> specifics, CancellationToken cancellationToken = default)
        {
            await _context.CategoryItemSpecifics.AddRangeAsync(specifics, cancellationToken);
        }
    }
}
