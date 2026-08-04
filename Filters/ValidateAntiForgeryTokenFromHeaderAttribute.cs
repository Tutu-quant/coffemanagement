using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Quản_lý_quán_cafe.Filters;

/// <summary>
/// Custom attribute để validate CSRF token từ header X-CSRF-TOKEN
/// Dùng cho JSON POST requests (FromBody)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ValidateAntiForgeryTokenFromHeaderAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new BadRequestResult();
        }
    }
}
