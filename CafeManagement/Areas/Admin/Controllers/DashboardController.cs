using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            ViewBag.RevenueToday = await _context.Orders
                .Where(o => o.Status == OrderStatus.DaThanhToan && o.PaidAt >= today)
                .SelectMany(o => o.OrderDetails)
                .SumAsync(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0;

            ViewBag.RevenueMonth = await _context.Orders
                .Where(o => o.Status == OrderStatus.DaThanhToan && o.PaidAt >= monthStart)
                .SelectMany(o => o.OrderDetails)
                .SumAsync(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0;

            ViewBag.OrdersTodayCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.DaThanhToan && o.PaidAt >= today);

            ViewBag.TotalTables = await _context.RestaurantTables.CountAsync();
            ViewBag.AvailableTables = await _context.RestaurantTables.CountAsync(t => t.Status == TableStatus.Trong);
            ViewBag.PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.ChoXacNhan);
            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            return View();
        }
    }
}
