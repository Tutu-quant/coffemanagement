using CafeManagement.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Controllers
{
    // Xem thực đơn - cho phép mọi người (kể cả chưa đăng nhập) xem
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            var query = _context.Products.Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = await query.OrderBy(p => p.Category!.Name).ThenBy(p => p.Name).ToListAsync();
            return View(products);
        }
    }
}
