// Cashier Area - POS Controller
namespace CafeManagement.Areas.Cashier.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Cashier")]
    public class POSController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
