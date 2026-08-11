using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class SessionAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public string? Roles { get; }

        public SessionAuthorizeAttribute(string? roles = null)
        {
            Roles = roles;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var userId = http.Session.GetInt32("UserId");

            if (userId == null)
            {
                context.Result = UnauthorizedResultFor(http);
                return;
            }

            var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.AsNoTracking()
                .Include(item => item.Role)
                .Include(item => item.Employee)
                .Include(item => item.Customer)
                .FirstOrDefaultAsync(item => item.UserID == userId.Value);
            var roleName = user?.Role?.RoleName ?? string.Empty;
            var staffAccountInvalid = roleName is "Admin" or "Cashier"
                && (user?.Employee is null || user.Employee.IsDeleted || !user.Employee.IsActive);
            var customerAccountInvalid = roleName == "Customer"
                && (user?.Customer is null || user.Customer.IsDeleted || !user.Customer.IsActive);
            if (user is null || user.IsDeleted || !user.IsActive || user.Role is null || user.Role.IsDeleted
                || staffAccountInvalid || customerAccountInvalid)
            {
                http.Session.Clear();
                context.Result = UnauthorizedResultFor(http);
                return;
            }

            http.Session.SetString("RoleName", roleName);

            if (!string.IsNullOrWhiteSpace(Roles))
            {
                var allowed = Roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(r => r.ToLowerInvariant())
                    .Contains(roleName.ToLowerInvariant());

                if (!allowed)
                {
                    // Forbidden for authenticated user without required role
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }
            }

            await next();
        }

        private static IActionResult UnauthorizedResultFor(HttpContext http) =>
            http.Request.Path.StartsWithSegments("/api")
                ? new UnauthorizedResult()
                : new RedirectToActionResult("Login", "Account", new { area = "" });
    }
}
