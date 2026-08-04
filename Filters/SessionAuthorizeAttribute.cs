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


            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }


            if (!string.IsNullOrWhiteSpace(Roles))
            {
                var roleName = http.Session.GetString("RoleName") ?? string.Empty;
                var allowed = Roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(r => r.ToLowerInvariant())
                    .Contains(roleName.ToLowerInvariant());

                if (!allowed)
                {

                    context.Result = new ForbidResult();
                    return;
                }
            }

            await next();
        }
    }
}
