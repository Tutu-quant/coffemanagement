using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Quản_lý_quán_cafe.Models.ViewModels.Product;
using Quản_lý_quán_cafe.Services.Interfaces;
using System.Security.Claims;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize] // TODO: Implement custom authorization using Session
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(IProductService productService, IWebHostEnvironment hostEnvironment)
        {
            _productService = productService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, int? category = null, string? status = null, string? sort = null)
        {
            ProductListViewModel model;

            if (!string.IsNullOrWhiteSpace(search) || category.HasValue || !string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(sort))
            {
                bool? isAvailable = null;
                if (status == "available")
                    isAvailable = true;
                else if (status == "unavailable")
                    isAvailable = false;

                var sortBy = sort ?? "name_asc";
                model = await _productService.SearchWithFilterAsync(search ?? string.Empty, category, isAvailable, sortBy, pageNumber, 10);
            }
            else
            {
                model = await _productService.GetAllAsync(pageNumber, 10);
            }

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            var categories = await _productService.GetAllCategoriesAsync();
            var model = new ProductCreateViewModel();
            ViewBag.Categories = categories;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            // Validate duplicate name
            var isNameValid = await _productService.ValidateNameAsync(model.Name);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm này đã tồn tại");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            // Validate price
            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            try
            {
                // Handle file upload
                if (model.ImageFile != null)
                {
                    // Validate file
                    var (isValid, errorMessage) = ValidateImageFile(model.ImageFile);
                    if (!isValid)
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        var categories = await _productService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        return View(model);
                    }

                    // Save file
                    var fileName = await SaveImageFile(model.ImageFile);
                    model.ImageFile = new FormFile(
                        new MemoryStream(await System.IO.File.ReadAllBytesAsync(
                            Path.Combine(_hostEnvironment.WebRootPath, "uploads", "products", fileName))),
                        0,
                        model.ImageFile.Length,
                        model.ImageFile.Name,
                        fileName
                    );
                }

                await _productService.CreateAsync(model);
                TempData["SuccessMessage"] = "Sản phẩm được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                IsAvailable = product.IsAvailable,
                ImageUrl = product.ImageUrl
            };

            var categories = await _productService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            // Validate duplicate name
            var isNameValid = await _productService.ValidateNameAsync(model.Name, id);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm này đã tồn tại");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            // Validate price
            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            try
            {
                // Handle file upload
                if (model.ImageFile != null)
                {
                    // Validate file
                    var (isValid, errorMessage) = ValidateImageFile(model.ImageFile);
                    if (!isValid)
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        var categories = await _productService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        return View(model);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                    {
                        DeleteImageFile(model.ImageUrl);
                    }

                    // Save new file
                    var fileName = await SaveImageFile(model.ImageFile);
                    model.ImageFile = new FormFile(
                        new MemoryStream(await System.IO.File.ReadAllBytesAsync(
                            Path.Combine(_hostEnvironment.WebRootPath, "uploads", "products", fileName))),
                        0,
                        model.ImageFile.Length,
                        model.ImageFile.Name,
                        fileName
                    );
                }

                await _productService.UpdateAsync(model);
                TempData["SuccessMessage"] = "Sản phẩm được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                TempData["SuccessMessage"] = "Sản phẩm được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            if (file == null)
                return (true, string.Empty);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Định dạng ảnh không hợp lệ. Chỉ chấp nhận: JPG, JPEG, PNG, WebP");
            }

            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
            {
                return (false, "Kích thước ảnh không được vượt quá 5MB");
            }

            return (true, string.Empty);
        }

        private async Task<string> SaveImageFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var filePath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "products", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
