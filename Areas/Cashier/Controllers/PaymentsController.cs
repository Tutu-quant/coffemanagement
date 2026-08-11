using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class PaymentsController : Controller
    {
        private readonly Data.ApplicationDbContext _context;

        public PaymentsController(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var start = BusinessClock.StartOfTodayUtc;
            var end = BusinessClock.StartOfTomorrowUtc;

            var paymentsToday = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.PaymentDate >= start && p.PaymentDate < end)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var model = new Areas.Cashier.ViewModels.PaymentsViewModel
            {
                TotalPaidToday = paymentsToday.Where(p => p.PaymentStatus == "Completed").Sum(p => p.Amount),
                PendingAmount = paymentsToday.Where(p => p.PaymentStatus is "Pending" or "Processing").Sum(p => p.Amount),
                TransactionCountToday = paymentsToday.Count,
                Transactions = paymentsToday.Select(p => new Areas.Cashier.ViewModels.TransactionItem
                {
                    OrderCode = $"#{p.OrderID:D6}",
                    PaymentMethod = p.PaymentMethod,
                    PaymentStatus = p.PaymentStatus,
                    Amount = p.Amount,
                    Time = BusinessClock.FromUtc(p.PaymentDate)
                }).ToList()
            };

            return View(model);
        }
    }
}
