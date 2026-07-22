using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Quản_lý_quán_cafe.Areas.Cashier
{
    public static class CashierAreaRoutes
    {
        public static void MapCashierArea(this WebApplication app)
        {
            app.MapAreaControllerRoute(
                name: "cashier_default",
                areaName: "Cashier",
                pattern: "Cashier/{controller=Dashboard}/{action=Index}/{id?}");
        }
    }
}
