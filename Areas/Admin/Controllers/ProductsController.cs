// Admin Area - Products Controller
namespace CafeManagement.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Admin")]
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
