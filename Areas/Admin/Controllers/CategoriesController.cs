// Admin Area - Categories Controller
namespace CafeManagement.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [Area("Admin")]
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
