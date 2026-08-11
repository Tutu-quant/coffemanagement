using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using TableStatuses = Quản_lý_quán_cafe.Models.Enums.TableStatus;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers;

[Area("Cashier")]
[SessionAuthorize("Cashier,Admin")]
public class TablesController(ApplicationDbContext context, IApplicationMutationCoordinator mutationCoordinator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsStaff()) return RedirectToAction("Login", "Account", new { area = "" });
        var now = BusinessClock.Now;
        var holdCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
        var oldestActiveStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
        var tables = await context.RestaurantTables.AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Include(t => t.Reservations.Where(r => !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" && r.ReservationStatus != "CheckedIn" &&
                r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart))
            .OrderBy(t => t.TableNumber)
            .ToListAsync();
        ViewBag.ReservedTableIds = tables
            .Where(t => t.TableStatus == TableStatuses.Available && t.Reservations.Count > 0)
            .Select(t => t.TableID)
            .ToHashSet();
        return View(tables);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int tableId, string status)
    {
        if (!IsStaff()) return Forbid();
        await using var mutationLock = await mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
        var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        IActionResult Error(string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            if (isAjax) return StatusCode(statusCode, new { success = false, message });
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);
        if (table is null) return NotFound();
        var canonicalStatus = TableStatuses.All.FirstOrDefault(s =>
            string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
        if (canonicalStatus is null) return Error("Trạng thái bàn không hợp lệ.");
        status = canonicalStatus;
        if (status == TableStatuses.Reserved)
            return Error("Trạng thái Đã đặt được hệ thống tự động quản lý theo lịch đặt bàn.");

        var openOrder = await context.Orders.Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted &&
                                      o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                                      o.OrderDetails.Any(d => !d.IsDeleted));
        var checkedInReservation = await context.Reservations.FirstOrDefaultAsync(r =>
            r.TableID == tableId && !r.IsDeleted && r.ReservationStatus == "CheckedIn");
        if (checkedInReservation is not null && openOrder is null && status != TableStatuses.Occupied)
            return Error("Khách đã nhận bàn. Hãy hoàn tất hoặc hủy nhận bàn trước khi giải phóng bàn.", StatusCodes.Status409Conflict);
        if (status == TableStatuses.Maintenance)
        {
            var oldestActiveStart = BusinessClock.Now.AddMinutes(-ReservationPolicy.DurationMinutes);
            var hasActiveOrFutureReservation = await context.Reservations.AnyAsync(r =>
                r.TableID == tableId && !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
                r.ReservationDate > oldestActiveStart);
            if (hasActiveOrFutureReservation)
                return Error("Bàn còn lịch đặt đang hiệu lực hoặc sắp tới nên chưa thể chuyển sang bảo trì.", StatusCodes.Status409Conflict);
        }
        if (openOrder is not null && status is not (TableStatuses.Occupied or TableStatuses.WaitingPayment))
            return Error("Bàn đang có đơn mở nên không thể chuyển sang trạng thái này.", StatusCodes.Status409Conflict);
        if (openOrder is null && status == TableStatuses.WaitingPayment)
            return Error("Bàn không có đơn mở để chuyển sang chờ thanh toán.", StatusCodes.Status409Conflict);
        if (openOrder is null && status is TableStatuses.Occupied or TableStatuses.Maintenance)
        {
            var now = BusinessClock.Now;
            var holdCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
            var oldestActiveStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
            var hasBlockingReservation = await context.Reservations.AnyAsync(r =>
                r.TableID == tableId && !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" && r.ReservationStatus != "CheckedIn" &&
                r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart);
            if (hasBlockingReservation)
                return Error("Bàn đang được giữ cho lịch đặt sắp đến.", StatusCodes.Status409Conflict);
        }
        if (openOrder is not null && status == TableStatuses.WaitingPayment)
            openOrder.OrderStatus = "WaitingPayment";
        else if (openOrder?.OrderStatus == "WaitingPayment" && status == TableStatuses.Occupied)
            openOrder.OrderStatus = "Pending";
        table.TableStatus = status;
        table.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        if (isAjax) return Json(new { success = true, message = $"Đã cập nhật bàn {table.TableNumber}." });
        TempData["SuccessMessage"] = $"Đã cập nhật bàn {table.TableNumber}.";
        return RedirectToAction(nameof(Index));
    }

    private bool IsStaff()
    {
        var role = HttpContext.Session.GetString("RoleName");
        return role is "Admin" or "Cashier";
    }
}
