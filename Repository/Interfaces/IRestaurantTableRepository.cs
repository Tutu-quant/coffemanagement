using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface IRestaurantTableRepository
    {
        Task<RestaurantTable?> GetByIdAsync(int id);
        Task<List<RestaurantTable>> GetAllAsync();
        Task<List<RestaurantTable>> SearchWithFilterAsync(string searchTerm, string? location, string? status, string sortBy, int skip = 0, int take = 10);
        Task<int> GetCountAsync();
        Task<int> GetCountByFilterAsync(string searchTerm, string? location, string? status);
        Task<int> GetCountByStatusAsync(string status);
        Task<List<string>> GetAllLocationsAsync();
        Task<List<RestaurantTable>> GetTablesByStatusAsync(string status);
        Task AddAsync(RestaurantTable table);
        Task UpdateAsync(RestaurantTable table);
        Task DeleteAsync(int id);
        Task<bool> ExistsByTableNumberAsync(string tableNumber, int? excludeId = null);
        Task<bool> CanDeleteAsync(int tableId);
    }
}
