using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Category;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<CategoryDetailViewModel?> GetByIdAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return null;

            return new CategoryDetailViewModel
            {
                Id = category.CategoryID,
                Name = category.CategoryName,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        public async Task<CategoryListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountAsync();
            var skip = (pageNumber - 1) * pageSize;
            var categories = await _repository.SearchAsync(string.Empty, skip, pageSize);

            return new CategoryListViewModel
            {
                Categories = categories.Select(c => new CategoryViewModel
                {
                    Id = c.CategoryID,
                    Name = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<CategoryListViewModel> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountBySearchAsync(searchTerm);
            var skip = (pageNumber - 1) * pageSize;
            var categories = await _repository.SearchAsync(searchTerm, skip, pageSize);

            return new CategoryListViewModel
            {
                Categories = categories.Select(c => new CategoryViewModel
                {
                    Id = c.CategoryID,
                    Name = c.CategoryName,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt ?? DateTime.UtcNow
                }).ToList(),
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<int> CreateAsync(CategoryCreateViewModel model)
        {
            var category = new Category
            {
                CategoryName = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            await _repository.AddAsync(category);
            return category.CategoryID;
        }

        public async Task UpdateAsync(CategoryEditViewModel model)
        {
            var category = await _repository.GetByIdAsync(model.Id);
            if (category != null)
            {
                category.CategoryName = model.Name;
                category.Description = model.Description;
                category.IsActive = model.IsActive;
                await _repository.UpdateAsync(category);
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<bool> ValidateNameAsync(string name, int? excludeId = null)
        {
            return !await _repository.ExistsByNameAsync(name, excludeId);
        }
    }
}
