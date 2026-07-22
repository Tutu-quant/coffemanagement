using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> GetAllAsync();
        Task<List<Product>> GetByCategoryAsync(int categoryId);
        Task<List<Product>> SearchAsync(string searchTerm, int skip = 0, int take = 10);
        Task<List<Product>> SearchWithFilterAsync(string searchTerm, int? categoryId, bool? isAvailable, string sortBy, int skip = 0, int take = 10);
        Task<int> GetCountAsync();
        Task<int> GetCountBySearchAsync(string searchTerm);
        Task<int> GetCountByFilterAsync(string searchTerm, int? categoryId, bool? isAvailable);
        Task<int> GetSalesCountAsync(int productId);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    }
}
