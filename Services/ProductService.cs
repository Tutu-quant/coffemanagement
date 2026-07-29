using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Product;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductService(IProductRepository repository, ICategoryRepository categoryRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
        }

        public async Task<ProductDetailViewModel?> GetByIdAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            if (product == null) return null;

            var salesCount = await _repository.GetSalesCountAsync(id);

            return new ProductDetailViewModel
            {
                Id = product.ProductID,
                Name = product.ProductName,
                Description = product.Description,
                CategoryId = product.CategoryID,
                CategoryName = product.Category?.CategoryName,
                Price = product.Price,
                SalesCount = salesCount,
                IsAvailable = product.IsActive,
                ImageUrl = product.ImageUrl,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }

        public async Task<ProductListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountAsync();
            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.SearchWithFilterAsync(string.Empty, null, null, "name_asc", skip, pageSize);
            var categories = await GetAllCategoriesAsync();

            var productVMs = new List<ProductViewModel>();
            foreach (var product in products)
            {
                var salesCount = await _repository.GetSalesCountAsync(product.ProductID);
                productVMs.Add(new ProductViewModel
                {
                    Id = product.ProductID,
                    Name = product.ProductName,
                    CategoryName = product.Category?.CategoryName,
                    CategoryId = product.CategoryID,
                    Price = product.Price,
                    SalesCount = salesCount,
                    IsAvailable = product.IsActive,
                    ImageUrl = product.ImageUrl,
                    CreatedAt = product.CreatedAt
                });
            }

            return new ProductListViewModel
            {
                Products = productVMs,
                Categories = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<ProductListViewModel> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountBySearchAsync(searchTerm);
            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.SearchAsync(searchTerm, skip, pageSize);
            var categories = await GetAllCategoriesAsync();

            var productVMs = new List<ProductViewModel>();
            foreach (var product in products)
            {
                var salesCount = await _repository.GetSalesCountAsync(product.ProductID);
                productVMs.Add(new ProductViewModel
                {
                    Id = product.ProductID,
                    Name = product.ProductName,
                    CategoryName = product.Category?.CategoryName,
                    CategoryId = product.CategoryID,
                    Price = product.Price,
                    SalesCount = salesCount,
                    IsAvailable = product.IsActive,
                    ImageUrl = product.ImageUrl,
                    CreatedAt = product.CreatedAt
                });
            }

            return new ProductListViewModel
            {
                Products = productVMs,
                Categories = categories,
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<ProductListViewModel> SearchWithFilterAsync(string searchTerm, int? categoryId, bool? isAvailable, string sortBy, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountByFilterAsync(searchTerm, categoryId, isAvailable);
            var skip = (pageNumber - 1) * pageSize;
            var products = await _repository.SearchWithFilterAsync(searchTerm, categoryId, isAvailable, sortBy, skip, pageSize);
            var categories = await GetAllCategoriesAsync();

            var productVMs = new List<ProductViewModel>();
            foreach (var product in products)
            {
                var salesCount = await _repository.GetSalesCountAsync(product.ProductID);
                productVMs.Add(new ProductViewModel
                {
                    Id = product.ProductID,
                    Name = product.ProductName,
                    CategoryName = product.Category?.CategoryName,
                    CategoryId = product.CategoryID,
                    Price = product.Price,
                    SalesCount = salesCount,
                    IsAvailable = product.IsActive,
                    ImageUrl = product.ImageUrl,
                    CreatedAt = product.CreatedAt
                });
            }

            return new ProductListViewModel
            {
                Products = productVMs,
                Categories = categories,
                SearchTerm = searchTerm,
                SelectedCategoryId = categoryId,
                SelectedStatus = isAvailable.HasValue ? (isAvailable.Value ? "available" : "unavailable") : null,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        public async Task<int> CreateAsync(ProductCreateViewModel model)
        {
            var product = new Product
            {
                ProductName = model.Name,
                Description = model.Description,
                CategoryID = model.CategoryId,
                Price = model.Price,
                IsActive = model.IsAvailable,
                ImageUrl = model.ImageFile?.FileName
            };

            await _repository.AddAsync(product);
            return product.ProductID;
        }

        public async Task UpdateAsync(ProductEditViewModel model)
        {
            var product = await _repository.GetByIdAsync(model.Id);
            if (product != null)
            {
                product.ProductName = model.Name;
                product.Description = model.Description;
                product.CategoryID = model.CategoryId;
                product.Price = model.Price;
                product.IsActive = model.IsAvailable;

                if (model.ImageFile != null)
                {
                    product.ImageUrl = model.ImageFile.FileName;
                }

                await _repository.UpdateAsync(product);
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

        public async Task<List<CategorySelectViewModel>> GetAllCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            var result = categories
                .Where(c => !c.IsDeleted)
                .Select(c => new CategorySelectViewModel
                {
                    Id = c.CategoryID,
                    Name = c.CategoryName
                })
                .ToList();
            return result;
        }
    }
}
