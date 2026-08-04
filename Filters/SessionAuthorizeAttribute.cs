using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            // Not authenticated -> redirect to Login
            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // If roles specified, check RoleName in session
            if (!string.IsNullOrWhiteSpace(Roles))
            {
                var roleName = http.Session.GetString("RoleName") ?? string.Empty;
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
    }
}
