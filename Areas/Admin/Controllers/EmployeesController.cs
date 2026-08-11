using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Employees;
using Quản_lý_quán_cafe.Repository;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
[SessionAuthorize("Admin")]
public class EmployeesController(ApplicationDbContext context, ILogger<EmployeesController> logger) : Controller
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string search = "", string sort = "newest", string department = "")
    {
        search = search?.Trim() ?? string.Empty;
        department = department?.Trim() ?? string.Empty;
        sort = sort is "name_asc" or "name_desc" or "newest" or "oldest" ? sort : "newest";
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.User)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => 
                e.FullName.Contains(search) || 
                (e.User != null && e.User.Username.Contains(search)) || 
                e.Email.Contains(search) ||
                (e.Phone != null && e.Phone.Contains(search)));
        }

        // Filter by department
        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.Department == department);
        }

        // Get all departments for filter dropdown
        var departments = await _context.Employees
            .Where(e => !e.IsDeleted)
            .Select(e => e.Department)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        // Sort
        query = sort switch
        {
            "name_asc" => query.OrderBy(e => e.FullName),
            "name_desc" => query.OrderByDescending(e => e.FullName),
            "newest" => query.OrderByDescending(e => e.CreatedAt),
            "oldest" => query.OrderBy(e => e.CreatedAt),
            _ => query.OrderByDescending(e => e.CreatedAt)
        };

        var employees = await query.ToListAsync();
        var totalEmployees = employees.Count;
        var activeEmployees = employees.Count(e => e.IsActive);
        var inactiveEmployees = totalEmployees - activeEmployees;

        var model = new EmployeeListViewModel
        {
            Employees = employees,
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            InactiveEmployees = inactiveEmployees,
            SearchTerm = search,
            SortBy = sort,
            Department = department,
            Departments = departments
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        model.Username = model.Username?.Trim() ?? string.Empty;
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        model.Position = model.Position?.Trim() ?? string.Empty;
        model.Department = model.Department?.Trim() ?? string.Empty;
        Revalidate(model, nameof(model.Username), nameof(model.FullName), nameof(model.Email), nameof(model.Position), nameof(model.Department));
        if (model.Username.Any(char.IsWhiteSpace))
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập không được chứa khoảng trắng.");
        if (!ModelState.IsValid)
            return View(model);

        // Kiểm tra username đã tồn tại
        var normalizedUsername = model.Username.ToLowerInvariant();
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

        if (existingUser != null)
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            return View(model);
        }

        if (await _context.Employees.AnyAsync(employee => !employee.IsDeleted &&
                employee.Email.ToLower() == normalizedUsername)
            || await _context.Customers.AnyAsync(customer => !customer.IsDeleted && customer.Email != null &&
                customer.Email.ToLower() == normalizedUsername))
        {
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập trùng với email đã được sử dụng");
            return View(model);
        }

        // Kiểm tra email đã tồn tại
        var existingEmail = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email.ToLower() == model.Email && !e.IsDeleted);

        var existingCustomerEmail = await _context.Customers
            .AnyAsync(c => c.Email != null && c.Email.ToLower() == model.Email && !c.IsDeleted);
        var emailMatchesUsername = await _context.Users
            .AnyAsync(user => !user.IsDeleted && user.Username.ToLower() == model.Email);

        if (existingEmail != null || existingCustomerEmail || emailMatchesUsername)
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng");
            return View(model);
        }

        var cashierRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == "Cashier" && !r.IsDeleted);
        if (cashierRole is null)
        {
            ModelState.AddModelError(string.Empty, "Hệ thống chưa có quyền Thu ngân.");
            return View(model);
        }

        var employee = new Employee
        {
            FullName = model.FullName,
            Email = model.Email,
            Position = model.Position,
            Department = string.IsNullOrWhiteSpace(model.Department) ? "Chưa cập nhật" : model.Department,
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
            HireDate = Models.BusinessClock.Today,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user = new User
        {
            Username = model.Username,
            PasswordHash = UserRepository.HashPassword(model.Password),
            RoleID = cashierRole.RoleID,
            Employee = employee,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not create employee account {Username}.", model.Username);
            ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản nhân viên. Vui lòng kiểm tra lại dữ liệu.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Tạo tài khoản nhân viên '{model.FullName}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        var model = new EmployeeDetailViewModel
        {
            EmployeeID = employee.EmployeeID,
            UserID = employee.User?.UserID ?? employee.UserID,
            FullName = employee.FullName,
            Email = employee.Email,
            Username = employee.User?.Username ?? "",
            Position = employee.Position,
            Department = employee.Department,
            Phone = employee.Phone ?? "",
            Address = employee.Address ?? "",
            IsActive = employee.IsActive,
            HireDate = employee.HireDate,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        var model = new EmployeeEditViewModel
        {
            EmployeeID = employee.EmployeeID,
            FullName = employee.FullName,
            Email = employee.Email,
            Position = employee.Position,
            Department = employee.Department,
            Phone = employee.Phone ?? "",
            Address = employee.Address ?? "",
            IsActive = employee.IsActive
        };
        ViewBag.IsCurrentAccount = employee.User?.UserID == HttpContext.Session.GetInt32("UserId");

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel model)
    {
        model.FullName = model.FullName?.Trim() ?? string.Empty;
        model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        model.Position = model.Position?.Trim() ?? string.Empty;
        model.Department = model.Department?.Trim() ?? string.Empty;
        model.Phone = model.Phone?.Trim() ?? string.Empty;
        model.Address = model.Address?.Trim() ?? string.Empty;
        Revalidate(model, nameof(model.FullName), nameof(model.Email), nameof(model.Position), nameof(model.Department), nameof(model.Phone), nameof(model.Address));

        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == model.EmployeeID && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        var isCurrentAccount = employee.User?.UserID == HttpContext.Session.GetInt32("UserId");
        ViewBag.IsCurrentAccount = isCurrentAccount;
        if (isCurrentAccount && !model.IsActive)
        {
            model.IsActive = true;
            ModelState.Remove(nameof(model.IsActive));
            ModelState.AddModelError(string.Empty, "Bạn không thể vô hiệu hóa tài khoản đang đăng nhập.");
        }

        if (!ModelState.IsValid)
            return View(model);

        // Check if email changed and is unique
        if (!string.Equals(employee.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existingEmail = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email.ToLower() == model.Email && e.EmployeeID != model.EmployeeID && !e.IsDeleted);
            var existingCustomerEmail = await _context.Customers
                .AnyAsync(c => c.Email != null && c.Email.ToLower() == model.Email && !c.IsDeleted);
            var emailMatchesUsername = await _context.Users
                .AnyAsync(user => !user.IsDeleted && user.Username.ToLower() == model.Email);
            if (existingEmail != null || existingCustomerEmail || emailMatchesUsername)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View(model);
            }
        }

        employee.FullName = model.FullName;
        employee.Email = model.Email;
        employee.Position = model.Position;
        employee.Department = string.IsNullOrWhiteSpace(model.Department) ? "Chưa cập nhật" : model.Department;
        employee.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone;
        employee.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address;
        employee.IsActive = model.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.User != null)
        {
            employee.User.IsActive = model.IsActive;
            employee.User.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not update employee {EmployeeId}.", model.EmployeeID);
            ModelState.AddModelError(string.Empty, "Không thể cập nhật nhân viên. Vui lòng thử lại.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        if (employee.User?.UserID == HttpContext.Session.GetInt32("UserId"))
        {
            TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.User != null)
        {
            employee.User.IsActive = false;
            employee.User.UpdatedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not delete employee {EmployeeId}.", id);
            TempData["ErrorMessage"] = "Không thể xóa nhân viên. Vui lòng thử lại.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = $"Đã xóa nhân viên '{employee.FullName}'";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        ViewBag.EmployeeID = id;
        ViewBag.EmployeeName = employee.FullName;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(int id, [FromForm] string newPassword, [FromForm] string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự!";
            return RedirectToAction(nameof(ChangePassword), new { id });
        }

        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] = "Mật khẩu không khớp!";
            return RedirectToAction(nameof(ChangePassword), new { id });
        }

        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        if (employee.User == null)
        {
            TempData["ErrorMessage"] = "Nhân viên này chưa có tài khoản đăng nhập.";
            return RedirectToAction(nameof(Index));
        }

        employee.User.PasswordHash = UserRepository.HashPassword(newPassword);
        employee.User.UpdatedAt = DateTime.UtcNow;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not change password for employee {EmployeeId}.", id);
            TempData["ErrorMessage"] = "Không thể đổi mật khẩu. Vui lòng thử lại.";
            return RedirectToAction(nameof(ChangePassword), new { id });
        }

        TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu cho '{employee.FullName}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    private void Revalidate(object model, params string[] fields)
    {
        foreach (var field in fields)
        {
            ModelState.Remove(field);
        }

        TryValidateModel(model);
    }
}
