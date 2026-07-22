using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductID == id && !p.IsDeleted);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> GetByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryID == categoryId && !p.IsDeleted)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<Product>> SearchAsync(string searchTerm, int skip = 0, int take = 10)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) || 
                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            return await query
                .OrderBy(p => p.ProductName)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Product>> SearchWithFilterAsync(string searchTerm, int? categoryId, bool? isAvailable, string sortBy, int skip = 0, int take = 10)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted);

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) || 
                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // Status filter
            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsActive == isAvailable.Value);
            }

            // Sorting
            query = sortBy switch
            {
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "date_newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.ProductName)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Products
                .Where(p => !p.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetCountBySearchAsync(string searchTerm)
        {
            var query = _context.Products
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) || 
                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            return await query.CountAsync();
        }

        public async Task<int> GetCountByFilterAsync(string searchTerm, int? categoryId, bool? isAvailable)
        {
            var query = _context.Products
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) || 
                                        (p.Description != null && p.Description.Contains(searchTerm)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsActive == isAvailable.Value);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetSalesCountAsync(int productId)
        {
            return await _context.OrderDetails
                .Where(od => od.ProductID == productId)
                .SumAsync(od => od.Quantity);
        }

        public async Task AddAsync(Product product)
        {
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await GetByIdAsync(id);
            if (product != null)
            {
                product.IsDeleted = true;
                await UpdateAsync(product);
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Products
                .Where(p => !p.IsDeleted && p.ProductName == name);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.ProductID != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
