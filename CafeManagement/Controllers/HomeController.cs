using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalTables = await _context.RestaurantTables.CountAsync();
            ViewBag.AvailableTables = await _context.RestaurantTables
                .CountAsync(t => t.Status == TableStatus.Trong);
            ViewBag.FeaturedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .Take(6)
                .ToListAsync();
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
