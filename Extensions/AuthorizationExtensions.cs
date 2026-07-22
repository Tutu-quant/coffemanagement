namespace CafeManagement.Extensions
{
    public static class AuthorizationExtensions
    {
        public static bool CanAccessAdminArea(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin") || user.IsInRole("Manager");
        }

        public static bool CanAccessCashierArea(this ClaimsPrincipal user)
        {
            return user.IsInRole("Cashier") || user.IsInRole("Admin") || user.IsInRole("Manager");
        }

        public static bool CanAccessCustomerArea(this ClaimsPrincipal user)
        {
            return user.IsInRole("Customer");
        }
    }
}
