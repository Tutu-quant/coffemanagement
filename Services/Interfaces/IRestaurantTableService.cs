using Quản_lý_quán_cafe.Models.ViewModels.RestaurantTable;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IRestaurantTableService
    {
        Task<RestaurantTableDetailViewModel?> GetByIdAsync(int id);
        Task<RestaurantTableListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<RestaurantTableListViewModel> SearchWithFilterAsync(string searchTerm, string? location, string? status, string sortBy, int pageNumber = 1, int pageSize = 10);
        Task<int> CreateAsync(RestaurantTableCreateViewModel model);
        Task UpdateAsync(RestaurantTableEditViewModel model);
        Task DeleteAsync(int id);
        Task<bool> ValidateTableNumberAsync(string tableNumber, int? excludeId = null);
        Task<List<string>> GetAllLocationsAsync();
        Task<Dictionary<string, int>> GetTableStatisticsAsync();
    }
}
