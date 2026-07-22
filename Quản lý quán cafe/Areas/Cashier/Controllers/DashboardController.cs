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
                var lastOrder = table.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault();

                tableStatuses.Add(new CashierDashboardViewModel.TableStatusDto
                {
                    TableID = table.TableID,
                    TableName = table.TableNumber,
                    Status = lastOrder == null
                        ? "Empty"
                        : lastOrder.OrderStatus == "Paid" ? "Empty" : (lastOrder.OrderStatus == "Completed" ? "WaitingPayment" : "Occupied"),
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
                .ToListAsync();

            var activeTablesCount = tableStatuses.Count(t => t.Status == "Occupied");
            var waitingPaymentCount = tableStatuses.Count(t => t.Status == "WaitingPayment");
            var todayRevenue = todayOrders.Where(o => o.OrderStatus == "Paid").Sum(o => o.TotalAmount);

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
                Tables = tableStatuses,
                RecentOrders = recentOrders
            };

            return View(viewModel);
        }
    }
}
