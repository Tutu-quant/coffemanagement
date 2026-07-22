using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.Customer;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _service;

        public CustomersController(ICustomerService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, string? tier = null, string? sort = null)
        {
            CustomerListViewModel model;

            if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrEmpty(tier) || !string.IsNullOrEmpty(sort))
            {
                var sortBy = sort ?? "newest";
                model = await _service.SearchWithFilterAsync(search ?? string.Empty, tier, sortBy, pageNumber, 10);
            }
            else
            {
                model = await _service.GetAllAsync(pageNumber, 10);
            }

            ViewData["Title"] = "Quản lý khách hàng";
            ViewData["PageTitle"] = "Khách hàng";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><i class='bi bi-people'></i> Khách hàng</span>";

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Chi tiết khách hàng";
            ViewData["PageTitle"] = "Chi tiết";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-people'></i> Khách hàng</a></span><span class='breadcrumb-item'><i class='bi bi-info-circle'></i> Chi tiết</span>";

            return View(customer);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm khách hàng";
            ViewData["PageTitle"] = "Thêm khách hàng";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-people'></i> Khách hàng</a></span><span class='breadcrumb-item'><i class='bi bi-plus-lg'></i> Thêm</span>";

            return View(new CustomerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate phone
            var isValidPhone = await _service.ValidatePhoneAsync(model.Phone);
            if (!isValidPhone)
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng");
                return View(model);
            }

            try
            {
                await _service.CreateAsync(model);
                TempData["SuccessMessage"] = "Khách hàng được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerEditViewModel
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                RewardPoints = customer.RewardPoints,
                MembershipTier = customer.MembershipTier,
                TotalSpent = customer.TotalSpent,
                IsActive = customer.IsActive
            };

            ViewData["Title"] = "Chỉnh sửa khách hàng";
            ViewData["PageTitle"] = "Chỉnh sửa";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-people'></i> Khách hàng</a></span><span class='breadcrumb-item'><i class='bi bi-pencil'></i> Chỉnh sửa</span>";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate phone
            var isValidPhone = await _service.ValidatePhoneAsync(model.Phone, id);
            if (!isValidPhone)
            {
                ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng");
                return View(model);
            }

            try
            {
                await _service.UpdateAsync(model);
                TempData["SuccessMessage"] = "Khách hàng được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra: " + ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Xóa khách hàng";
            ViewData["PageTitle"] = "Xóa";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-people'></i> Khách hàng</a></span><span class='breadcrumb-item'><i class='bi bi-trash'></i> Xóa</span>";

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                TempData["SuccessMessage"] = "Khách hàng được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}
