using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Admin.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;

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

        var paymentAccount = await _context.PaymentAccountSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Provider == "Placeholder");

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
            LowStockItems = lowStockItems,
            PaymentAccount = paymentAccount is null
                ? new PaymentAccountViewModel()
                : new PaymentAccountViewModel
                {
                    AccountNumber = paymentAccount.AccountNumber,
                    AccountName = paymentAccount.AccountName,
                    IsActive = paymentAccount.IsActive
                }
        };

        return View(viewModel);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePaymentAccount(
        [Bind(Prefix = "PaymentAccount")] PaymentAccountViewModel model)
    {
        if (HttpContext.Session.GetString("RoleName") != "Admin")
            return Forbid();

        if (!ModelState.IsValid)
        {
            TempData["PaymentAccountError"] = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index), new { paymentSettings = true });
        }

        var setting = await _context.PaymentAccountSettings
            .FirstOrDefaultAsync(x => x.Provider == "Placeholder");
        if (setting is null)
        {
            setting = new PaymentAccountSetting
            {
                Provider = "Placeholder",
                CreatedAt = DateTime.UtcNow
            };
            _context.PaymentAccountSettings.Add(setting);
        }

        setting.AccountNumber = model.AccountNumber.Trim();
        setting.AccountName = model.AccountName.Trim();
        setting.IsActive = model.IsActive;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = HttpContext.Session.GetString("Username");
        await _context.SaveChangesAsync();
        TempData["PaymentAccountSuccess"] = "Đã cập nhật placeholder tài khoản nhận tiền.";
        return RedirectToAction(nameof(Index), new { paymentSettings = true });
    }
}
