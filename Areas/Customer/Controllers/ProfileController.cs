using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.ViewModels.Customer;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Filters;
using Microsoft.EntityFrameworkCore;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
[SessionAuthorize("Customer")]
public class ProfileController(
    ApplicationDbContext context,
    CustomerSessionService customerSessionService,
    ILoyaltyService loyaltyService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly CustomerSessionService _customerSessionService = customerSessionService;
    private readonly ILoyaltyService _loyaltyService = loyaltyService;

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var sessionCustomer = await _customerSessionService.GetOrCreateCustomerAsync();
        var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(item =>
            item.CustomerID == sessionCustomer.CustomerID && !item.IsDeleted && item.IsActive);
        if (customer is null) return Unauthorized();

        var model = new CustomerEditViewModel
        {
            Id = customer.CustomerID,
            Name = customer.CustomerName ?? string.Empty,
            Phone = customer.Phone ?? string.Empty,
            Email = customer.Email,
            Address = customer.Address
        };

        ViewBag.Customer = customer;

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        if (!IsCustomer()) return RedirectToLogin();

        var sessionCustomer = await _customerSessionService.GetOrCreateCustomerAsync();

        if (model.Id != sessionCustomer.CustomerID)
            return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Customer = sessionCustomer;
            return View(model);
        }

        var customer = await _context.Customers.FirstOrDefaultAsync(item =>
            item.CustomerID == sessionCustomer.CustomerID && !item.IsDeleted && item.IsActive);
        if (customer is null) return Unauthorized();

        var normalizedEmail = string.IsNullOrWhiteSpace(model.Email)
            ? null : model.Email.Trim().ToLowerInvariant();
        if (normalizedEmail is not null &&
            (await _context.Customers.AnyAsync(item => item.CustomerID != customer.CustomerID &&
                item.Email != null && item.Email.ToLower() == normalizedEmail)
             || await _context.Employees.AnyAsync(item => item.Email.ToLower() == normalizedEmail)
             || await _context.Users.AnyAsync(item => item.Username.ToLower() == normalizedEmail)))
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            ViewBag.Customer = customer;
            return View(model);
        }

        var normalizedPhone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        if (normalizedPhone is not null && await _context.Customers.AnyAsync(item =>
                item.CustomerID != customer.CustomerID && item.Phone == normalizedPhone))
        {
            ModelState.AddModelError(nameof(model.Phone), "Số điện thoại đã được sử dụng.");
            ViewBag.Customer = customer;
            return View(model);
        }

        customer.CustomerName = model.Name?.Trim() ?? customer.CustomerName;
        customer.Phone = normalizedPhone;
        customer.Email = normalizedEmail;
        customer.Address = model.Address?.Trim() ?? customer.Address;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        HttpContext.Session.SetString("FullName", customer.CustomerName);

        TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";

        return RedirectToAction(nameof(Edit));
    }

    [HttpGet]
    public async Task<IActionResult> Points()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var summary = await _loyaltyService.GetCustomerSummaryAsync(
            customer.CustomerID,
            cancellationToken: HttpContext.RequestAborted);
        var model = new CustomerPointsViewModel
        {
            CustomerId = customer.CustomerID,
            CustomerName = customer.CustomerName,
            RewardPoints = summary.RewardPoints,
            History = summary.History
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
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        return View(customer);
    }

    private bool IsCustomer() =>
        (HttpContext.Session.GetInt32("UserId") ?? 0) > 0
        && HttpContext.Session.GetString("RoleName") == "Customer";

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "Account", new { area = "" });
}
