using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Quản_lý_quán_cafe.Models.ViewModels.Product;
using Quản_lý_quán_cafe.Services.Interfaces;
using System.Security.Claims;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuthorize("Admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, IWebHostEnvironment hostEnvironment, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, int? category = null, string? status = null, string? sort = null)
        {
            pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
            search = search?.Trim();
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

            if (model.TotalPages > 0 && pageNumber > model.TotalPages)
                return RedirectToAction(nameof(Index), new { pageNumber = model.TotalPages, search, category, status, sort });

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
            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            if (!ModelState.IsValid)
            {
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            var activeCategories = await _productService.GetAllCategoriesAsync();
            if (!activeCategories.Any(category => category.Id == model.CategoryId))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không còn hoạt động.");
                ViewBag.Categories = activeCategories;
                return View(model);
            }


            var isNameValid = await _productService.ValidateNameAsync(model.Name);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm này đã tồn tại");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }


            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            string? savedFileName = null;
            try
            {

                if (model.ImageFile != null)
                {

                    var (isValid, errorMessage) = ValidateImageFile(model.ImageFile);
                    if (!isValid)
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        var categories = await _productService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        return View(model);
                    }


                    var fileName = await SaveImageFile(model.ImageFile);
                    savedFileName = fileName;
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
                if (savedFileName is not null) DeleteImageFile(savedFileName);
                _logger.LogError(ex, "Could not create product {ProductName}.", model.Name);
                ModelState.AddModelError(string.Empty, "Không thể tạo sản phẩm. Vui lòng thử lại.");
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
                Quantity = product.Quantity,
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

            model.Name = model.Name?.Trim() ?? string.Empty;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            var currentProduct = await _productService.GetByIdAsync(id);
            if (currentProduct is null) return NotFound();
            model.ImageUrl = currentProduct.ImageUrl;

            if (!ModelState.IsValid)
            {
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            var activeCategories = await _productService.GetAllCategoriesAsync();
            if (!activeCategories.Any(category => category.Id == model.CategoryId))
            {
                ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không còn hoạt động.");
                ViewBag.Categories = activeCategories;
                return View(model);
            }


            var isNameValid = await _productService.ValidateNameAsync(model.Name, id);
            if (!isNameValid)
            {
                ModelState.AddModelError("Name", "Tên sản phẩm này đã tồn tại");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }


            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "Giá sản phẩm phải lớn hơn 0");
                var categories = await _productService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(model);
            }

            string? savedFileName = null;
            try
            {

                if (model.ImageFile != null)
                {

                    var (isValid, errorMessage) = ValidateImageFile(model.ImageFile);
                    if (!isValid)
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        var categories = await _productService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        return View(model);
                    }


                    var fileName = await SaveImageFile(model.ImageFile);
                    savedFileName = fileName;
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
                if ((savedFileName is not null || model.RemoveImage) && !string.IsNullOrWhiteSpace(currentProduct.ImageUrl))
                    DeleteImageFile(currentProduct.ImageUrl);
                TempData["SuccessMessage"] = "Sản phẩm được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (savedFileName is not null) DeleteImageFile(savedFileName);
                _logger.LogError(ex, "Could not update product {ProductId}.", id);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật sản phẩm. Vui lòng thử lại.");
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
                var product = await _productService.GetByIdAsync(id);
                if (product is null) return NotFound();
                await _productService.DeleteAsync(id);
                if (!string.IsNullOrWhiteSpace(product.ImageUrl)) DeleteImageFile(product.ImageUrl);
                TempData["SuccessMessage"] = "Sản phẩm được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete product {ProductId}.", id);
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile file)
        {
            if (file == null)
                return (true, string.Empty);

            if (file.Length <= 0)
                return (false, "Tệp ảnh đang trống.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return (false, "Định dạng ảnh không hợp lệ. Chỉ chấp nhận: JPG, JPEG, PNG, WebP");
            }

            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return (false, "Kích thước ảnh không được vượt quá 5MB");
            }

            Span<byte> header = stackalloc byte[12];
            using var stream = file.OpenReadStream();
            var bytesRead = stream.Read(header);
            var validSignature = fileExtension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => bytesRead >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
                _ => false
            };
            if (!validSignature)
                return (false, "Nội dung tệp không đúng định dạng ảnh đã chọn.");

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

            var uploadsFolder = Path.GetFullPath(Path.Combine(_hostEnvironment.WebRootPath, "uploads", "products"));
            var safeName = Path.GetFileName(fileName);
            if (!string.Equals(safeName, fileName, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(safeName))
                return;
            var filePath = Path.GetFullPath(Path.Combine(uploadsFolder, safeName));
            if (!string.Equals(Path.GetDirectoryName(filePath), uploadsFolder, StringComparison.OrdinalIgnoreCase))
                return;
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
