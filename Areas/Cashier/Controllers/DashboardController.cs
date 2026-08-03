using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Data;
using Microsoft.EntityFrameworkCore;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
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
            if (!IsStaff())
                return RedirectToAction("Login", "Account", new { area = "" });

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Get all restaurant tables with order info
            var tables = await _context.RestaurantTables
                .Where(t => !t.IsDeleted)
                .Include(t => t.Orders.Where(o => !o.IsDeleted && o.OrderDate >= today && o.OrderDate < tomorrow))
                    .ThenInclude(o => o.OrderDetails)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // Build table status DTOs
            var tableStatuses = new List<CashierDashboardViewModel.TableStatusDto>();
            foreach (var table in tables)
            {
                var lastOrder = table.Orders
                    .Where(o => o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled")
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefault();

                var displayStatus = table.TableStatus switch
                {
                    "Reserved" => "Reserved",
                    "WaitingPayment" => "WaitingPayment",
                    _ when lastOrder != null => "Occupied",
                    _ => "Empty"
                };

                tableStatuses.Add(new CashierDashboardViewModel.TableStatusDto
                {
                    TableID = table.TableID,
                    TableName = table.TableNumber,
                    Status = displayStatus,
                    OrderID = lastOrder?.OrderID,
                    OrderCode = $"{lastOrder?.OrderID:D6}",
                    GuestCount = lastOrder?.OrderDetails.Count,
                    StartTime = lastOrder?.OrderDate,
                    TotalAmount = lastOrder?.TotalAmount
                });
            }

            // Get today's stats
            var todayOrders = await _context.Orders
                .Where(o => !o.IsDeleted && o.OrderDate >= today && o.OrderDate < tomorrow)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .ToListAsync();

            var todayRevenue = await _context.Payments
                .Where(p => !p.IsDeleted && p.PaymentStatus == "Completed" &&
                            p.PaymentDate >= today && p.PaymentDate < tomorrow)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var pendingBills = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.TableID != null &&
                            o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                            o.OrderDetails.Any(d => !d.IsDeleted))
                .Include(o => o.Table)
                .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .OrderBy(o => o.OrderDate)
                .Select(o => new CashierDashboardViewModel.OrderSummaryDto
                {
                    OrderID = o.OrderID,
                    OrderCode = $"{o.OrderID:D6}",
                    TableID = o.TableID!.Value,
                    TableName = o.Table != null ? o.Table.TableNumber : "N/A",
                    TotalAmount = o.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal),
                    Status = o.OrderStatus,
                    CreatedAt = o.OrderDate,
                    ItemCount = o.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Quantity)
                })
                .ToListAsync();

            var activeTablesCount = tableStatuses.Count(t => t.Status == "Occupied");
            var waitingPaymentCount = pendingBills.Count;

            // Get recent orders
            var recentOrders = todayOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new CashierDashboardViewModel.OrderSummaryDto
                {
                    OrderID = o.OrderID,
                    OrderCode = $"{o.OrderID:D6}",
                    TableID = o.TableID ?? 0,
                    TableName = o.Table?.TableNumber ?? "N/A",
                    TotalAmount = o.TotalAmount,
                    Status = o.OrderStatus,
                    CreatedAt = o.OrderDate,
                    ItemCount = o.OrderDetails.Count
                })
                .ToList();

            var viewModel = new CashierDashboardViewModel
            {
                ActiveTablesCount = activeTablesCount,
                TodayOrdersCount = todayOrders.Count,
                WaitingPaymentCount = waitingPaymentCount,
                TodayRevenue = todayRevenue,
                PendingPaymentTotal = pendingBills.Sum(o => o.TotalAmount),
                Tables = tableStatuses,
                RecentOrders = recentOrders,
                PendingBills = pendingBills
            };

            return View(viewModel);
        }

        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("RoleName");
            return role is "Admin" or "Cashier";
        }
    }
}
