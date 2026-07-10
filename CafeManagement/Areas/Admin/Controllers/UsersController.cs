using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public class UserRow
        {
            public ApplicationUser User { get; set; } = null!;
            public IList<string> Roles { get; set; } = new List<string>();
        }

        // Danh sách toàn bộ user + role hiện tại
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.OrderBy(u => u.Email).ToList();
            var rows = new List<UserRow>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                rows.Add(new UserRow { User = u, Roles = roles });
            }
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(rows);
        }

        // Gán / thay đổi role cho 1 user (Admin / Staff / Customer)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                TempData["Error"] = "Vai trò không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);

            TempData["Success"] = $"Đã cập nhật vai trò của {user.Email} thành {role}.";
            return RedirectToAction(nameof(Index));
        }

        // Khoá / mở khoá tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            user.LockoutEnd = user.IsActive ? null : DateTimeOffset.MaxValue;
            user.LockoutEnabled = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = user.IsActive ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.";
            return RedirectToAction(nameof(Index));
        }
    }
}
