using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.RestaurantTable;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuthorize("Admin")]
    public class RestaurantTablesController : Controller
    {
        private readonly IRestaurantTableService _service;
        private readonly ILogger<RestaurantTablesController> _logger;

        public RestaurantTablesController(IRestaurantTableService service, ILogger<RestaurantTablesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, string? location = null, string? status = null, string? sort = null)
        {
            pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
            search = search?.Trim();
            RestaurantTableListViewModel model;

            if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(location) || !string.IsNullOrWhiteSpace(status) || !string.IsNullOrEmpty(sort))
            {
                var sortBy = sort ?? "name_asc";
                model = await _service.SearchWithFilterAsync(search ?? string.Empty, location, status, sortBy, pageNumber, 12);
            }
            else
            {
                model = await _service.GetAllAsync(pageNumber, 12);
            }

            if (model.TotalPages > 0 && pageNumber > model.TotalPages)
                return RedirectToAction(nameof(Index), new { pageNumber = model.TotalPages, search, location, status, sort });

            ViewData["Title"] = "Quản lý bàn";
            ViewData["PageTitle"] = "Bàn";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><i class='bi bi-grid-3x3'></i> Bàn</span>";

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm bàn";
            ViewData["PageTitle"] = "Thêm bàn";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-grid-3x3'></i> Bàn</a></span><span class='breadcrumb-item'><i class='bi bi-plus-lg'></i> Thêm</span>";

            var locations = await _service.GetAllLocationsAsync();
            ViewBag.Locations = locations;

            return View(new RestaurantTableCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RestaurantTableCreateViewModel model)
        {
            model.TableNumber = model.TableNumber?.Trim() ?? string.Empty;
            model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
            if (!ModelState.IsValid)
            {
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }


            var isValid = await _service.ValidateTableNumberAsync(model.TableNumber);
            if (!isValid)
            {
                ModelState.AddModelError("TableNumber", "Mã bàn này đã tồn tại");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }


            if (model.Capacity <= 0)
            {
                ModelState.AddModelError("Capacity", "Sức chứa phải lớn hơn 0");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            try
            {
                await _service.CreateAsync(model);
                TempData["SuccessMessage"] = "Bàn được tạo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not create restaurant table {TableNumber}.", model.TableNumber);
                ModelState.AddModelError(string.Empty, "Không thể tạo bàn. Vui lòng thử lại.");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var table = await _service.GetByIdAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            var model = new RestaurantTableEditViewModel
            {
                Id = table.Id,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                TableStatus = table.TableStatus,
                Location = table.Location
            };

            ViewData["Title"] = "Chỉnh sửa bàn";
            ViewData["PageTitle"] = "Chỉnh sửa bàn";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-grid-3x3'></i> Bàn</a></span><span class='breadcrumb-item'><i class='bi bi-pencil'></i> Chỉnh sửa</span>";

            var locations = await _service.GetAllLocationsAsync();
            ViewBag.Locations = locations;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RestaurantTableEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            model.TableNumber = model.TableNumber?.Trim() ?? string.Empty;
            model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();

            if (!ModelState.IsValid)
            {
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }


            var isValid = await _service.ValidateTableNumberAsync(model.TableNumber, id);
            if (!isValid)
            {
                ModelState.AddModelError("TableNumber", "Mã bàn này đã tồn tại");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }


            if (model.Capacity <= 0)
            {
                ModelState.AddModelError("Capacity", "Sức chứa phải lớn hơn 0");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            try
            {
                await _service.UpdateAsync(model);
                TempData["SuccessMessage"] = "Bàn được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Locations = await _service.GetAllLocationsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update restaurant table {TableId}.", id);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật bàn. Vui lòng thử lại.");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var table = await _service.GetByIdAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Chi tiết bàn";
            ViewData["PageTitle"] = "Chi tiết bàn";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-grid-3x3'></i> Bàn</a></span><span class='breadcrumb-item'><i class='bi bi-info-circle'></i> Chi tiết</span>";

            return View(table);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var table = await _service.GetByIdAsync(id);
            if (table == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Xóa bàn";
            ViewData["PageTitle"] = "Xóa bàn";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-grid-3x3'></i> Bàn</a></span><span class='breadcrumb-item'><i class='bi bi-trash'></i> Xóa</span>";

            return View(table);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData["SuccessMessage"] = "Bàn được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete restaurant table {TableId}.", id);
                TempData["ErrorMessage"] = "Không thể xóa bàn. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
