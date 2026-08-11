using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/reports")]
[SessionAuthorize("Admin")]
public class ReportsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (HttpContext.Session.GetString("RoleName") != "Admin") return StatusCode(403);
        var localStart = (from ?? BusinessClock.Today.AddDays(-29)).Date;
        var localEnd = (to ?? BusinessClock.Today).Date;
        if (localStart > localEnd || (localEnd - localStart).TotalDays > 365)
            return BadRequest(new { message = "Khoảng thời gian không hợp lệ." });
        var startUtc = BusinessClock.ToUtc(localStart);
        var endUtc = BusinessClock.ToUtc(localEnd.AddDays(1));
        var payments = context.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.PaymentStatus == "Completed" && p.PaymentDate >= startUtc && p.PaymentDate < endUtc);
        var paymentRows = await payments.Select(payment => new { payment.PaymentDate, payment.Amount }).ToListAsync();
        var total = paymentRows.Sum(payment => payment.Amount);
        var count = paymentRows.Count;
        var byDay = paymentRows
            .GroupBy(payment => BusinessClock.FromUtc(payment.PaymentDate).Date)
            .Select(group => new { Date = group.Key, Revenue = group.Sum(payment => payment.Amount), Orders = group.Count() })
            .OrderBy(item => item.Date)
            .ToList();
        var bestSellers = await context.OrderDetails.AsNoTracking().Where(d => !d.IsDeleted && d.Order != null
                && !d.Order.IsDeleted && d.Order.OrderStatus == "Completed"
                && d.Order.CompletedDate >= startUtc && d.Order.CompletedDate < endUtc)
            .GroupBy(d => new { d.ProductID, d.Product!.ProductName }).Select(g => new { g.Key.ProductID, g.Key.ProductName, Quantity = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.Subtotal) }).OrderByDescending(x => x.Quantity).Take(10).ToListAsync();
        return Ok(new { From = localStart, To = localEnd, TotalRevenue = total, OrderCount = count, AverageOrderValue = count == 0 ? 0 : total / count, ByDay = byDay, BestSellers = bestSellers });
    }
}
