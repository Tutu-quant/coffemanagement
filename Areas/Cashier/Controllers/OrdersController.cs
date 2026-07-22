// Cashier Area - Orders Controller
namespace CafeManagement.Areas.Cashier.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Cashier")]
    public class OrdersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
