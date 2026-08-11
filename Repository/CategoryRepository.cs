using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == id && !c.IsDeleted);
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        public async Task<List<Category>> SearchAsync(string searchTerm, int skip = 0, int take = 10)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CategoryName.Contains(searchTerm) ||
                                        (c.Description != null && c.Description.Contains(searchTerm)));
            }

            return await query
                .OrderBy(c => c.CategoryName)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Categories
                .Where(c => !c.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetCountBySearchAsync(string searchTerm)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CategoryName.Contains(searchTerm) ||
                                        (c.Description != null && c.Description.Contains(searchTerm)));
            }

            return await query.CountAsync();
        }

        public async Task AddAsync(Category category)
        {
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            var entry = _context.Entry(category);
            if (entry.State == EntityState.Detached)
                _context.Categories.Attach(category);
            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await GetByIdAsync(id);
            if (category != null)
            {
                category.IsDeleted = true;
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;
                var products = await _context.Products
                    .Where(product => product.CategoryID == id && !product.IsDeleted)
                    .ToListAsync();
                foreach (var product in products)
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted && c.CategoryName == name);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryID != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}

