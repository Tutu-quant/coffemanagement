// Customer Area - Home Controller
namespace CafeManagement.Areas.Customer.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Customer")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
