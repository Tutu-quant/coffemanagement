using CafeManagement.Models.Entities;

namespace CafeManagement.Repositories.Interfaces
{
    public interface ITableRepository
    {
        Task<IEnumerable<RestaurantTable>> GetAllAsync();
        Task<RestaurantTable> GetByIdAsync(int id);
        Task<IEnumerable<RestaurantTable>> GetAvailableAsync();
        Task AddAsync(RestaurantTable table);
        Task UpdateAsync(RestaurantTable table);
        Task DeleteAsync(int id);
        Task SaveAsync();
    }
}
