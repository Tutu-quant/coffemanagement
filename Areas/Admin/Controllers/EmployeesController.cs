using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Admin.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
public class EmployeesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var query = context.Employees.AsNoTracking().Include(e => e.Users).Where(e => !e.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.FullName.Contains(search) || (e.Email ?? "").Contains(search) || (e.Phone ?? "").Contains(search));
        ViewBag.Search = search;
        return View(await query.OrderBy(e => e.FullName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var employee = await context.Employees.AsNoTracking().Include(e => e.Users)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);
        return employee is null ? NotFound() : View(employee);
    }

    public IActionResult Create()
    {
        if (!IsAdmin()) return RedirectToLogin();
        return View(new EmployeeFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (!IsAdmin()) return Forbid();
        if (!ModelState.IsValid) return View(model);
        var employee = new Employee { CreatedAt = DateTime.UtcNow };
        Apply(model, employee);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã thêm nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var e = await context.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeID == id && !x.IsDeleted);
        return e is null ? NotFound() : View(new EmployeeFormViewModel
        {
            EmployeeID = e.EmployeeID, FullName = e.FullName, Gender = e.Gender, BirthDate = e.BirthDate,
            Phone = e.Phone, Email = e.Email, Address = e.Address, HireDate = e.HireDate, Salary = e.Salary
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeFormViewModel model)
    {
        if (!IsAdmin()) return Forbid();
        if (id != model.EmployeeID) return NotFound();
        if (!ModelState.IsValid) return View(model);
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);
        if (employee is null) return NotFound();
        Apply(model, employee);
        employee.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin()) return Forbid();
        var employee = await context.Employees.Include(e => e.Users).FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);
        if (employee is null) return NotFound();
        if (employee.Users.Any(u => !u.IsDeleted && u.IsActive))
        {
            TempData["ErrorMessage"] = "Không thể xóa nhân viên đang có tài khoản hoạt động.";
            return RedirectToAction(nameof(Details), new { id });
        }
        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa nhân viên.";
        return RedirectToAction(nameof(Index));
    }

    private static void Apply(EmployeeFormViewModel m, Employee e)
    {
        e.FullName = m.FullName.Trim(); e.Gender = m.Gender; e.BirthDate = m.BirthDate;
        e.Phone = m.Phone?.Trim(); e.Email = m.Email?.Trim(); e.Address = m.Address?.Trim();
        e.HireDate = m.HireDate; e.Salary = m.Salary;
    }
    private bool IsAdmin() => HttpContext.Session.GetString("RoleName") == "Admin";
    private IActionResult RedirectToLogin() => RedirectToAction("Login", "Account", new { area = "" });
}
