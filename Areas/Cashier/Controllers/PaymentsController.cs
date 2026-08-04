using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
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
            var start = DateTime.UtcNow.Date;
            var end = start.AddDays(1);

            var paymentsToday = await _context.Payments
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.CreatedAt >= start && p.CreatedAt < end)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var model = new Areas.Cashier.ViewModels.PaymentsViewModel
            {
                TotalPaidToday = paymentsToday.Where(p => p.PaymentStatus == "Completed").Sum(p => p.Amount),
                PendingAmount = paymentsToday.Where(p => p.PaymentStatus != "Completed").Sum(p => p.Amount),
                TransactionCountToday = paymentsToday.Count,
                Transactions = paymentsToday.Select(p => new Areas.Cashier.ViewModels.TransactionItem
                {
                    OrderCode = $"#{p.OrderID:D6}",
                    PaymentMethod = p.PaymentMethod,
                    Amount = p.Amount,
                    Time = p.CreatedAt.ToLocalTime()
                }).ToList()
            };

            return View(model);
        }
    }
}
