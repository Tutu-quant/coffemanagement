using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.RestaurantTable;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RestaurantTablesController : Controller
    {
        private readonly IRestaurantTableService _service;

        public RestaurantTablesController(IRestaurantTableService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, string? location = null, string? status = null, string? sort = null)
        {
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
            if (!ModelState.IsValid)
            {
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            // Validate duplicate table number
            var isValid = await _service.ValidateTableNumberAsync(model.TableNumber);
            if (!isValid)
            {
                ModelState.AddModelError("TableNumber", "Mã bàn này đã tồn tại");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            // Validate capacity
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
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
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

            if (!ModelState.IsValid)
            {
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            // Validate duplicate table number
            var isValid = await _service.ValidateTableNumberAsync(model.TableNumber, id);
            if (!isValid)
            {
                ModelState.AddModelError("TableNumber", "Mã bàn này đã tồn tại");
                var locations = await _service.GetAllLocationsAsync();
                ViewBag.Locations = locations;
                return View(model);
            }

            // Validate capacity
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
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
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
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
