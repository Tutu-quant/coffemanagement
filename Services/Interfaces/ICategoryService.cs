using Quản_lý_quán_cafe.Models.ViewModels.Category;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<CategoryDetailViewModel?> GetByIdAsync(int id);
        Task<CategoryListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<CategoryListViewModel> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
        Task<int> CreateAsync(CategoryCreateViewModel model);
        Task UpdateAsync(CategoryEditViewModel model);
        Task DeleteAsync(int id);
        Task<bool> ValidateNameAsync(string name, int? excludeId = null);
    }
}
