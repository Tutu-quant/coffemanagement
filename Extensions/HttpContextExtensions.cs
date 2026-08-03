using Microsoft.AspNetCore.Http;
using System;

namespace Quản_lý_quán_cafe.Extensions
{
    public static class HttpContextExtensions
    {
        public static int? GetCurrentUserId(this HttpContext http)
            => http.Session.GetInt32("UserId");

        public static int? GetCurrentRoleId(this HttpContext http)
            => http.Session.GetInt32("RoleId");

        public static string? GetCurrentRoleName(this HttpContext http)
            => http.Session.GetString("RoleName");

        public static string? GetCurrentUsername(this HttpContext http)
            => http.Session.GetString("Username");

        public static bool IsInRole(this HttpContext http, string role)
            => string.Equals(GetCurrentRoleName(http) ?? string.Empty, role, StringComparison.OrdinalIgnoreCase);
    }
}
