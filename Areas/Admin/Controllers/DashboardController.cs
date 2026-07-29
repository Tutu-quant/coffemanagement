using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Admin.ViewModels;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("RoleName") != "Admin")
            return RedirectToAction("Login", "Account", new { area = "" });

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var sevenDaysAgo = today.AddDays(-6);
        var yesterday = today.AddDays(-1);

        var completedOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.OrderStatus == "Completed"
                && o.OrderDate >= sevenDaysAgo)
            .Select(o => new { o.OrderDate, o.TotalAmount })
            .ToListAsync();

        var revenuePoints = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = sevenDaysAgo.AddDays(offset);
                var orders = completedOrders.Where(o => o.OrderDate.Date == date);
                return new RevenuePoint(date, orders.Sum(o => o.TotalAmount), orders.Count());
            })
            .ToList();

        var todayRevenue = revenuePoints.Last().Revenue;
        var yesterdayRevenue = revenuePoints[^2].Revenue;
        var growth = yesterdayRevenue == 0
            ? (todayRevenue > 0 ? 100 : 0)
            : Math.Round((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100, 1);

        var monthRevenue = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted
                && o.OrderStatus == "Completed"
                && o.OrderDate >= monthStart
                && o.OrderDate < tomorrow)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var bestSellerRows = await _context.OrderDetails
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                && d.Order != null
                && !d.Order.IsDeleted
                && d.Order.OrderStatus == "Completed"
                && d.Order.OrderDate >= monthStart)
            .GroupBy(d => new { d.ProductID, d.Product!.ProductName })
            .Select(g => new
            {
                g.Key.ProductID,
                g.Key.ProductName,
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(5)
            .ToListAsync();
        var bestSellers = bestSellerRows
            .Select(x => new BestSellerItem(x.ProductID, x.ProductName, x.Quantity, x.Revenue))
            .ToList();

        var recentOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate)
            .Take(8)
            .Select(o => new RecentOrderItem(
                o.OrderID,
                o.Table != null ? o.Table.TableNumber : "Mang đi",
                o.Customer != null ? o.Customer.CustomerName : "Khách lẻ",
                o.TotalAmount,
                o.OrderStatus,
                o.OrderDate))
            .ToListAsync();

        var lowStockItems = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsActive && p.Quantity <= 10)
            .OrderBy(p => p.Quantity)
            .Take(6)
            .Select(p => new LowStockItem(p.ProductID, p.ProductName, p.Quantity))
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            TodayRevenue = todayRevenue,
            MonthRevenue = monthRevenue,
            TodayOrders = await _context.Orders.CountAsync(o =>
                !o.IsDeleted && o.OrderDate >= today && o.OrderDate < tomorrow),
            ActiveTables = await _context.RestaurantTables.CountAsync(t =>
                !t.IsDeleted && t.TableStatus != "Available"),
            TotalCustomers = await _context.Customers.CountAsync(c => !c.IsDeleted && c.IsActive),
            LowStockProducts = await _context.Products.CountAsync(p =>
                !p.IsDeleted && p.IsActive && p.Quantity <= 10),
            PendingReservations = await _context.Reservations.CountAsync(r =>
                !r.IsDeleted
                && r.ReservationDate >= today
                && r.ReservationDate < tomorrow
                && (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed")),
            RevenueGrowthPercent = growth,
            RevenueLast7Days = revenuePoints,
            BestSellers = bestSellers,
            RecentOrders = recentOrders,
            LowStockItems = lowStockItems
        };

        return View(viewModel);
    }
}
