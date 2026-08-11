using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Account;
using Quản_lý_quán_cafe.Repository;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace Quản_lý_quán_cafe.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAccountService accountService,
            IUserRepository userRepository,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }


        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-auth")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.Username = model.Username?.Trim() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _accountService.LoginAsync(model);
                if (!result.Success)
                {

                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(model);
                }

                HttpContext.Session.Clear();

                if (result.UserId.HasValue)
                    HttpContext.Session.SetInt32("UserId", result.UserId.Value);
                if (result.RoleId.HasValue)
                    HttpContext.Session.SetInt32("RoleId", result.RoleId.Value);
                if (!string.IsNullOrEmpty(result.RoleName))
                    HttpContext.Session.SetString("RoleName", result.RoleName);
                HttpContext.Session.SetString("Username", model.Username);
                var sessionUser = result.UserId.HasValue
                    ? await _userRepository.GetByIdAsync(result.UserId.Value)
                    : null;
                var fullName = sessionUser?.Employee?.FullName ?? sessionUser?.Customer?.CustomerName ?? model.Username;
                HttpContext.Session.SetString("FullName", fullName);
                if (sessionUser?.CustomerID is int customerId)
                    HttpContext.Session.SetInt32("CustomerId", customerId);


                TempData["SuccessMessage"] = result.Message ?? "Đăng nhập thành công";


                return result.RoleName?.ToLowerInvariant() switch
                {
                    "admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
                    "customer" => RedirectToAction("Menu", "Orders", new { area = "Customer" }),
                    _ => RedirectToAction("Index", "POS", new { area = "Cashier" })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while signing in as {Login}.", model.Username);
                ModelState.AddModelError(string.Empty, "Lỗi khi đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("account-auth")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.Username = model.Username?.Trim() ?? string.Empty;
            model.FullName = model.FullName?.Trim() ?? string.Empty;
            model.Email = model.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var normalizedUsername = model.Username.ToLowerInvariant();
            var existingUser = await _context.Users.AnyAsync(user =>
                user.Username.ToLower() == normalizedUsername);
            if (existingUser)
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Tên đăng nhập đã tồn tại");
                return View(model);
            }
            if (await _context.Employees.AnyAsync(employee => employee.Email.ToLower() == normalizedUsername)
                || await _context.Customers.AnyAsync(customer => customer.Email != null &&
                    customer.Email.ToLower() == normalizedUsername))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Username), "Tên đăng nhập trùng với email đã được sử dụng");
                return View(model);
            }
            if (await _context.Customers.AnyAsync(customer => customer.Email != null && customer.Email.ToLower() == model.Email)
                || await _context.Employees.AnyAsync(employee => employee.Email.ToLower() == model.Email)
                || await _context.Users.AnyAsync(user => user.Username.ToLower() == model.Email))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Email), "Email đã được sử dụng");
                return View(model);
            }
            var normalizedPhone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            if (normalizedPhone is not null && await _context.Customers.AnyAsync(customer =>
                    customer.Phone == normalizedPhone))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.Phone), "Số điện thoại đã được sử dụng");
                return View(model);
            }

            try
            {

                var allRoles = await _userRepository.GetAllRolesAsync();
                var customerRole = allRoles?.FirstOrDefault(r => r.RoleName.ToLower() == "customer");

                if (customerRole == null)
                {
                    _logger.LogError("Customer role was not found while registering {Username}.", model.Username);
                    ModelState.AddModelError(string.Empty, "Hệ thống chưa được cấu hình quyền Customer. Vui lòng liên hệ quản trị viên.");
                    return View(model);
                }


                var newUser = new User
                {
                    Username = model.Username.Trim(),
                    PasswordHash = UserRepository.HashPassword(model.Password),
                    RoleID = customerRole.RoleID,
                    Customer = new Customer
                    {
                        CustomerName = model.FullName.Trim(),
                        Email = model.Email.Trim().ToLowerInvariant(),
                        Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _userRepository.AddAsync(newUser);


                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogWarning(dbEx, "Database rejected registration for {Username}.", model.Username);
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài khoản. Vui lòng thử lại.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering {Username}.", model.Username);
                ModelState.AddModelError(string.Empty, "Lỗi khi tạo tài khoản. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
            return RedirectToAction("Login");
        }
    }
}
