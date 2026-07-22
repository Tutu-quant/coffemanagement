// Admin Area - Dashboard Controller
namespace CafeManagement.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
