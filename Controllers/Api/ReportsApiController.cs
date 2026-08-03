using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/reports")]
public class ReportsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (HttpContext.Session.GetString("RoleName") != "Admin") return StatusCode(403);
        var start = (from ?? DateTime.Today.AddDays(-29)).Date; var end = (to ?? DateTime.Today).Date.AddDays(1);
        if (start >= end || (end - start).TotalDays > 366) return BadRequest(new { message = "Khoảng thời gian không hợp lệ." });
        var payments = context.Payments.AsNoTracking().Where(p => !p.IsDeleted && p.PaymentStatus == "Completed" && p.PaymentDate >= start && p.PaymentDate < end);
        var total = await payments.SumAsync(p => (decimal?)p.Amount) ?? 0; var count = await payments.CountAsync();
        var byDay = await payments.GroupBy(p => p.PaymentDate.Date).Select(g => new { Date = g.Key, Revenue = g.Sum(p => p.Amount), Orders = g.Count() }).OrderBy(x => x.Date).ToListAsync();
        var bestSellers = await context.OrderDetails.AsNoTracking().Where(d => !d.IsDeleted && d.Order!.OrderStatus == "Completed" && d.Order.CompletedDate >= start && d.Order.CompletedDate < end)
            .GroupBy(d => new { d.ProductID, d.Product!.ProductName }).Select(g => new { g.Key.ProductID, g.Key.ProductName, Quantity = g.Sum(d => d.Quantity), Revenue = g.Sum(d => d.Subtotal) }).OrderByDescending(x => x.Quantity).Take(10).ToListAsync();
        return Ok(new { From = start, To = end.AddDays(-1), TotalRevenue = total, OrderCount = count, AverageOrderValue = count == 0 ? 0 : total / count, ByDay = byDay, BestSellers = bestSellers });
    }
}
