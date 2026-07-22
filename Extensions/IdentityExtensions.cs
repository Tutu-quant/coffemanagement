using Microsoft.AspNetCore.Identity;
using CafeManagement.Models.Entities;

namespace CafeManagement.Extensions
{
    public static class IdentityExtensions
    {
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }

        public static bool IsManager(this ClaimsPrincipal user)
        {
            return user.IsInRole("Manager") || user.IsInRole("Admin");
        }

        public static bool IsCashier(this ClaimsPrincipal user)
        {
            return user.IsInRole("Cashier");
        }

        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        }
    }
}
