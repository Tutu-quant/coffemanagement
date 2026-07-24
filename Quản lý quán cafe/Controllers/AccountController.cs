using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Account;
using Quản_lý_quán_cafe.Repository;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IUserRepository _userRepository;

        public AccountController(IAccountService accountService, IUserRepository userRepository)
        {
            _accountService = accountService;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Authenticate user
            var user = await _userRepository.AuthenticateAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác");
                return View(model);
            }

            // Get role from database
            var role = await _userRepository.GetRoleByIdAsync(user.RoleID);
            if (role == null)
            {
                ModelState.AddModelError(string.Empty, "Vị trí không hợp lệ");
                return View(model);
            }

            // Set session
            HttpContext.Session.SetInt32("UserId", user.UserID);
            HttpContext.Session.SetString("Username", model.Username);
            HttpContext.Session.SetInt32("RoleId", user.RoleID);
            HttpContext.Session.SetString("RoleName", role.RoleName ?? "");
            HttpContext.Session.SetString("FullName", user.Username);

            // Redirect based on role from database
            return role.RoleName?.ToLowerInvariant() switch
            {
                "admin" => RedirectToAction("Index", "RestaurantTables", new { area = "Admin" }),
                "customer" => RedirectToAction("Index", "Reservations", new { area = "Customer" }),
                _ => RedirectToAction("Index", "POS", new { area = "Cashier" })
            };
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username already exists
            var existingUser = await _userRepository.GetByUsernameAsync(model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            try
            {
                // Get Customer role
                var allRoles = await _userRepository.GetAllRolesAsync();
                var customerRole = allRoles?.FirstOrDefault(r => r.RoleName.ToLower() == "customer");

                if (customerRole == null)
                {
                    // Log detailed error
                    System.Diagnostics.Debug.WriteLine("ERROR: Customer role not found in database. Available roles: " + 
                        string.Join(", ", allRoles?.Select(r => r.RoleName) ?? new List<string>()));

                    ModelState.AddModelError(string.Empty, "Hệ thống chưa được cấu hình quyền Customer. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }

                // Create new user with Customer role
                var newUser = new User
                {
                    Username = model.Username,
                    PasswordHash = UserRepository.HashPassword(model.Password),
                    RoleID = customerRole.RoleID,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _userRepository.AddAsync(newUser);

                // Show success message
                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"Database Error during registration: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài khoản. Vui lòng thử lại.");
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error during registration: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài khoản. Vui lòng thử lại.");
                return View(model);
            }
        }

        public IActionResult GenerateHash()
        {
            return Content(UserRepository.HashPassword("123456"));
        }

        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
