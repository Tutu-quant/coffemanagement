using Quản_lý_quán_cafe.Models.ViewModels.Product;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductDetailViewModel?> GetByIdAsync(int id);
        Task<ProductListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<ProductListViewModel> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);
        Task<ProductListViewModel> SearchWithFilterAsync(string searchTerm, int? categoryId, bool? isAvailable, string sortBy, int pageNumber = 1, int pageSize = 10);
        Task<int> CreateAsync(ProductCreateViewModel model);
        Task UpdateAsync(ProductEditViewModel model);
        Task DeleteAsync(int id);
        Task<bool> ValidateNameAsync(string name, int? excludeId = null);
        Task<List<CategorySelectViewModel>> GetAllCategoriesAsync();
    }
}
