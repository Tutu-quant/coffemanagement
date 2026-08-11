using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.Customer;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Data;
using Microsoft.EntityFrameworkCore;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuthorize("Admin")]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _service;
        private readonly ILoyaltyService _loyaltyService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(
            ICustomerService service,
            ILoyaltyService loyaltyService,
            ApplicationDbContext context,
            ILogger<CustomersController> logger)
        {
            _service = service;
            _loyaltyService = loyaltyService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int pageNumber = 1, string? search = null, string? sort = null)
        {
            pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
            search = search?.Trim();
            CustomerListViewModel model;

            if (!string.IsNullOrWhiteSpace(search) || !string.IsNullOrEmpty(sort))
            {
                var sortBy = sort ?? "newest";
                model = await _service.SearchWithFilterAsync(search ?? string.Empty, sortBy, pageNumber, 10);
            }
            else
            {
                model = await _service.GetAllAsync(pageNumber, 10);
            }

            var customerIds = model.Customers.Select(item => item.Id).ToList();
            if (customerIds.Count > 0)
            {
                var accountData = await _context.Customers
                    .AsNoTracking()
                    .Where(item => customerIds.Contains(item.CustomerID))
                    .Select(item => new
                    {
                        item.CustomerID,
                        item.RewardPoints,
                        Username = item.User == null ? null : item.User.Username
                    })
                    .ToDictionaryAsync(item => item.CustomerID);
                foreach (var customer in model.Customers)
                {
                    if (!accountData.TryGetValue(customer.Id, out var account)) continue;
                    customer.RewardPoints = account.RewardPoints;
                    customer.Username = account.Username;
                }
            }
            model.TotalPoints = await _context.Customers
                .Where(item => !item.IsDeleted)
                .SumAsync(item => (long)item.RewardPoints);

            if (model.TotalPages > 0 && pageNumber > model.TotalPages)
                return RedirectToAction(nameof(Index), new { pageNumber = model.TotalPages, search, sort });

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

            var summary = await _loyaltyService.GetCustomerSummaryAsync(
                id,
                cancellationToken: HttpContext.RequestAborted);
            customer.RewardPoints = summary.RewardPoints;
            customer.Username = await _context.Users
                .AsNoTracking()
                .Where(item => !item.IsDeleted && item.CustomerID == id)
                .Select(item => item.Username)
                .FirstOrDefaultAsync();
            customer.PointHistory = summary.History
                .Select(item => new PointHistoryItemViewModel
                {
                    Id = item.PointHistoryId,
                    Points = item.Points,
                    BalanceAfter = item.BalanceAfter,
                    TransactionType = item.TransactionType,
                    Description = item.Description,
                    OrderId = item.OrderId,
                    TransactionDate = item.TransactionDate
                })
                .ToList();

            ViewData["Title"] = "Chi tiết khách hàng";
            ViewData["PageTitle"] = "Chi tiết";
            ViewData["PageBreadcrumb"] = "<span class='breadcrumb-item'><a href='" + Url.Action("Index") + "'><i class='bi bi-people'></i> Khách hàng</a></span><span class='breadcrumb-item'><i class='bi bi-info-circle'></i> Chi tiết</span>";

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GiftPoints(int id, GiftPointsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = ModelState.Values
                    .SelectMany(item => item.Errors)
                    .Select(item => item.ErrorMessage)
                    .FirstOrDefault() ?? "Số điểm tặng không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            try
            {
                await _loyaltyService.GiftPointsAsync(
                    id,
                    model.Points,
                    string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim(),
                    actorUserId,
                    HttpContext.RequestAborted);
                TempData["SuccessMessage"] = $"Đã tặng {model.Points:N0} điểm cho khách hàng.";
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or KeyNotFoundException)
            {
                _logger.LogWarning(ex, "Could not gift points to customer {CustomerId}.", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
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
                _logger.LogError(ex, "Could not create customer.");
                ModelState.AddModelError(string.Empty, ex is InvalidOperationException
                    ? ex.Message : "Không thể tạo khách hàng. Vui lòng thử lại.");
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
                Phone = customer.Phone ?? string.Empty,
                Email = customer.Email,
                Address = customer.Address,
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
                _logger.LogError(ex, "Could not update customer {CustomerId}.", id);
                ModelState.AddModelError(string.Empty, ex is InvalidOperationException
                    ? ex.Message : "Không thể cập nhật khách hàng. Vui lòng thử lại.");
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
                _logger.LogError(ex, "Could not delete customer {CustomerId}.", id);
                TempData["ErrorMessage"] = ex is InvalidOperationException
                    ? ex.Message : "Không thể xóa khách hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}
