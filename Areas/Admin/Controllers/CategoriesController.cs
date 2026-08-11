using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.Category;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuthorize("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            ICategoryService categoryService,
            ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null)
        {
            pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            CategoryListViewModel model;

            if (!string.IsNullOrWhiteSpace(search))
            {
                model = await _categoryService.SearchAsync(search, pageNumber, 10);
            }
            else
            {
                model = await _categoryService.GetAllAsync(pageNumber, 10);
            }

            if (model.TotalPages > 0 && pageNumber > model.TotalPages)
            {
                pageNumber = model.TotalPages;
                model = search is not null
                    ? await _categoryService.SearchAsync(search, pageNumber, 10)
                    : await _categoryService.GetAllAsync(pageNumber, 10);
            }

            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            Normalize(model);
            Revalidate(model, nameof(model.Name), nameof(model.Description));
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var isNameValid = await _categoryService.ValidateNameAsync(model.Name);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại");
                return View(model);
            }

            try
            {
                await _categoryService.CreateAsync(model);
                TempData["SuccessMessage"] = "Danh mục được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not create category {CategoryName}.", model.Name);
                ModelState.AddModelError(string.Empty, "Không thể tạo danh mục. Vui lòng thử lại.");
                return View(model);
            }

        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            Normalize(model);
            Revalidate(model, nameof(model.Name), nameof(model.Description));
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _categoryService.GetByIdAsync(id) is null)
            {
                return NotFound();
            }

            var isNameValid = await _categoryService.ValidateNameAsync(model.Name, id);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại");
                return View(model);
            }

            try
            {
                await _categoryService.UpdateAsync(model);
                TempData["SuccessMessage"] = "Danh mục được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not update category {CategoryId}.", id);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật danh mục. Vui lòng thử lại.");
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (await _categoryService.GetByIdAsync(id) is null)
            {
                return NotFound();
            }

            try
            {
                await _categoryService.DeleteAsync(id);
                TempData["SuccessMessage"] = "Danh mục được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not delete category {CategoryId}.", id);
                TempData["ErrorMessage"] = "Không thể xóa danh mục. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        private void Revalidate(object model, params string[] fields)
        {
            foreach (var field in fields)
            {
                ModelState.Remove(field);
            }

            TryValidateModel(model);
        }

        private static void Normalize(CategoryCreateViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        }

        private static void Normalize(CategoryEditViewModel model)
        {
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        }
    }
}
