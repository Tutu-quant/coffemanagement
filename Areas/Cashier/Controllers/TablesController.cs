using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using TableStatuses = Quản_lý_quán_cafe.Models.Enums.TableStatus;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers;

[Area("Cashier")]
public class TablesController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsStaff()) return RedirectToAction("Login", "Account", new { area = "" });
        return View(await context.RestaurantTables.AsNoTracking().Where(t => !t.IsDeleted).OrderBy(t => t.TableNumber).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int tableId, string status)
    {
        if (!IsStaff()) return Forbid();
        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);
        if (table is null) return NotFound();
        if (!TableStatuses.IsValid(status))
        {
            TempData["ErrorMessage"] = "Trạng thái bàn không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }
        var hasOpenOrder = await context.Orders.AnyAsync(o => o.TableID == tableId && !o.IsDeleted && o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled");
        if (hasOpenOrder && status is not (TableStatuses.Occupied or TableStatuses.WaitingPayment))
        {
            TempData["ErrorMessage"] = "Bàn đang có đơn mở nên không thể chuyển sang trạng thái này.";
            return RedirectToAction(nameof(Index));
        }
        table.TableStatus = status;
        table.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật bàn {table.TableNumber}.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsStaff()
    {
        var role = HttpContext.Session.GetString("RoleName");
        return role is "Admin" or "Cashier";
    }
}
