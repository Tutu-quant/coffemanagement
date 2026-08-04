using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
<<<<<<< HEAD
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Employees;
using Quản_lý_quán_cafe.Repository;
=======
using Quản_lý_quán_cafe.Areas.Admin.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
<<<<<<< HEAD
[SessionAuthorize("Admin")]
public class EmployeesController(ApplicationDbContext context) : Controller
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string search = "", string sort = "newest", string department = "")
    {
        var query = _context.Employees
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
        if (!ModelState.IsValid)
            return View(model);

        // Kiểm tra username đã tồn tại
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == model.Username);

        if (existingUser != null)
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            return View(model);
        }

        // Kiểm tra email đã tồn tại
        var existingEmail = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == model.Email && !e.IsDeleted);

        if (existingEmail != null)
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng");
            return View(model);
        }

        // Tạo User account cho nhân viên
        var user = new User
        {
            Username = model.Username,
            PasswordHash = UserRepository.HashPassword(model.Password),
            RoleID = (await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Employee"))?.RoleID ?? 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Tạo Employee record
        var employee = new Employee
        {
            UserID = user.UserID,
            FullName = model.FullName,
            Email = model.Email,
            Position = model.Position,
            Department = model.Department ?? "Chưa cập nhật",
            Phone = model.Phone ?? "",
            Address = model.Address ?? "",
            HireDate = DateTime.Now,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

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
            UserID = employee.UserID,
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

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel model)
    {
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == model.EmployeeID && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        // Check if email changed and is unique
        if (employee.Email != model.Email)
        {
            var existingEmail = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == model.Email && e.EmployeeID != model.EmployeeID && !e.IsDeleted);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng");
                return View(model);
            }
        }

        employee.FullName = model.FullName;
        employee.Email = model.Email;
        employee.Position = model.Position;
        employee.Department = model.Department ?? "Chưa cập nhật";
        employee.Phone = model.Phone ?? "";
        employee.Address = model.Address ?? "";
        employee.IsActive = model.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.User != null)
        {
            employee.User.IsActive = model.IsActive;
            employee.User.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(employee.User);
        }

        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
=======
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
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
<<<<<<< HEAD
        var employee = await _context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.EmployeeID == id && !e.IsDeleted);

        if (employee == null)
            return NotFound();

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;

        if (employee.User != null)
        {
            employee.User.IsActive = false;
            employee.User.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(employee.User);
        }

        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();

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

        if (employee.User != null)
        {
            employee.User.PasswordHash = UserRepository.HashPassword(newPassword);
            employee.User.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(employee.User);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu cho '{employee.FullName}' thành công!";
        return RedirectToAction(nameof(Index));
    }
=======
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
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
}
