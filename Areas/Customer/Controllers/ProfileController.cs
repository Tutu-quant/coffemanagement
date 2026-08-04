using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.ViewModels.Customer;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
public class ProfileController(ApplicationDbContext context, CustomerSessionService customerSessionService) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly CustomerSessionService _customerSessionService = customerSessionService;

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        var model = new CustomerEditViewModel
        {
            Id = customer.CustomerID,
            Name = customer.CustomerName,
            Phone = customer.Phone,
            Address = customer.Address
        };

        ViewBag.Customer = customer;

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        if (!IsCustomer()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        if (model.Id != customer.CustomerID)
            return Unauthorized();

        if (!ModelState.IsValid)
        {
            ViewBag.Customer = customer;
            return View(model);
        }

        customer.CustomerName = model.Name?.Trim() ?? customer.CustomerName;
        customer.Phone = model.Phone?.Trim() ?? customer.Phone;
        customer.Address = model.Address?.Trim() ?? customer.Address;
        customer.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";

        return RedirectToAction(nameof(Edit));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsCustomer()) return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Points()
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
