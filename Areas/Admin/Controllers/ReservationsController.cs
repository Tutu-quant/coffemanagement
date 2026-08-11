using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
[SessionAuthorize("Admin")]
public class ReservationsController(
    ApplicationDbContext context,
    IApplicationMutationCoordinator mutationCoordinator,
    ILogger<ReservationsController> logger) : Controller
{
    public async Task<IActionResult> Index(string? status, DateTime? date)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var query = context.Reservations.AsNoTracking().Include(r => r.Customer).Include(r => r.Table).Where(r => !r.IsDeleted);
        var validStatuses = new[] { "Pending", "Confirmed", "CheckedIn", "Completed", "Cancelled" };
        status = validStatuses.Contains(status, StringComparer.Ordinal) ? status : null;
        if (status is not null) query = query.Where(r => r.ReservationStatus == status);
        if (date.HasValue && date.Value.Date < DateTime.MaxValue.Date)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(r => r.ReservationDate >= start && r.ReservationDate < end);
        }
        else if (date.HasValue)
        {
            date = null;
        }
        ViewBag.Status = status; ViewBag.Date = date?.ToString("yyyy-MM-dd");
        return View(await query.OrderByDescending(r => r.ReservationDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var item = await context.Reservations.AsNoTracking().Include(r => r.Customer).Include(r => r.Table)
            .FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        return item is null ? NotFound() : View(item);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var item = await context.Reservations.FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        if (item is null) return NotFound();
        if (item.ReservationStatus is not ("Pending" or "Confirmed"))
        {
            TempData["ErrorMessage"] = "Chỉ có thể sửa lịch đặt đang chờ hoặc đã xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }
        await LoadTablesAsync(item.TableID);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ReservationID,TableID,ReservationDate,NumberOfGuests,Notes")] Reservation model)
    {
        if (!IsAdmin()) return Forbid();
        if (id != model.ReservationID) return NotFound();
        await using var mutationLock = await mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
        var item = await context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        if (item is null) return NotFound();
        if (item.ReservationStatus is not ("Pending" or "Confirmed"))
        {
            TempData["ErrorMessage"] = "Chỉ có thể sửa lịch đặt đang chờ hoặc đã xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == model.TableID && !t.IsDeleted);
        if (table is null)
            ModelState.AddModelError(nameof(model.TableID), "Bàn không tồn tại.");
        else if (model.NumberOfGuests < 1 || model.NumberOfGuests > table.Capacity)
            ModelState.AddModelError(nameof(model.NumberOfGuests), "Số khách không phù hợp sức chứa bàn.");
        if (model.ReservationDate <= BusinessClock.Now)
            ModelState.AddModelError(nameof(model.ReservationDate), "Thời gian đặt phải ở tương lai.");
        if (table?.TableStatus == "Maintenance")
            ModelState.AddModelError(nameof(model.TableID), "Bàn đang bảo trì.");
        if (table is not null && model.ReservationDate <= BusinessClock.Now.AddMinutes(ReservationPolicy.DurationMinutes) &&
            table.TableStatus is "Occupied" or "WaitingPayment")
            ModelState.AddModelError(nameof(model.TableID), "Bàn đang được sử dụng hoặc chờ thanh toán trong khung giờ này.");
        model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        if (model.Notes?.Length > 500)
            ModelState.AddModelError(nameof(model.Notes), "Ghi chú không được vượt quá 500 ký tự.");
        var conflict = false;
        var latestValidStart = DateTime.MaxValue.AddMinutes(-ReservationPolicy.DurationMinutes);
        if (model.ReservationDate > latestValidStart)
        {
            ModelState.AddModelError(nameof(model.ReservationDate), "Thời gian đặt không hợp lệ.");
        }
        else if (table is not null && model.ReservationDate > BusinessClock.Now)
        {
            var reservationEnd = model.ReservationDate.AddMinutes(ReservationPolicy.DurationMinutes);
            var earliestConflictingStart = model.ReservationDate.AddMinutes(-ReservationPolicy.DurationMinutes);
            conflict = await context.Reservations.AnyAsync(r => r.ReservationID != id && !r.IsDeleted && r.TableID == model.TableID &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
                r.ReservationDate > earliestConflictingStart && r.ReservationDate < reservationEnd);
        }
        if (conflict) ModelState.AddModelError(nameof(model.TableID), "Bàn đã có lịch đặt gần thời gian này.");
        if (!ModelState.IsValid)
        {
            model.ReservationStatus = item.ReservationStatus;
            model.CustomerID = item.CustomerID;
            await LoadTablesAsync(model.TableID);
            return View(model);
        }
        item.TableID = model.TableID; item.ReservationDate = model.ReservationDate; item.ReservationTime = model.ReservationDate; item.NumberOfGuests = model.NumberOfGuests;
        item.Notes = model.Notes; item.UpdatedAt = DateTime.UtcNow;
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not update reservation {ReservationId}.", id);
            ModelState.AddModelError(string.Empty, "Không thể cập nhật lịch đặt. Vui lòng thử lại.");
            model.ReservationStatus = item.ReservationStatus;
            model.CustomerID = item.CustomerID;
            await LoadTablesAsync(model.TableID);
            return View(model);
        }
        TempData["SuccessMessage"] = "Đã cập nhật lịch đặt bàn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Confirm(int id) => ChangeStatus(id, "Confirmed");
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id) => ChangeStatus(id, "Cancelled");
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CheckIn(int id) => ChangeStatus(id, "CheckedIn");
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(int id) => ChangeStatus(id, "Completed");

    private async Task<IActionResult> ChangeStatus(int id, string status)
    {
        if (!IsAdmin()) return Forbid();
        await using var mutationLock = await mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
        var item = await context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        if (item is null) return NotFound();
        var transitionAllowed = (item.ReservationStatus, status) switch
        {
            ("Pending", "Confirmed" or "Cancelled") => true,
            ("Confirmed", "CheckedIn" or "Cancelled") => true,
            ("CheckedIn", "Completed") => true,
            _ => false
        };
        if (!transitionAllowed)
        {
            TempData["ErrorMessage"] = $"Không thể chuyển từ {item.ReservationStatus} sang {status}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var openOrder = await context.Orders.FirstOrDefaultAsync(o => o.TableID == item.TableID && !o.IsDeleted &&
            o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" && o.OrderDetails.Any(d => !d.IsDeleted));
        var hasOpenOrder = openOrder is not null;
        if (status == "CheckedIn" && openOrder?.CustomerID is int orderCustomerId && orderCustomerId != item.CustomerID)
        {
            TempData["ErrorMessage"] = "Bàn đang có đơn mở nên không thể nhận lịch đặt này.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (status == "CheckedIn")
        {
            var now = BusinessClock.Now;
            if (item.ReservationDate > now.AddMinutes(ReservationPolicy.HoldBeforeMinutes) ||
                item.ReservationDate <= now.AddMinutes(-ReservationPolicy.DurationMinutes))
            {
                TempData["ErrorMessage"] = "Chỉ có thể nhận bàn trong khung giờ của lịch đặt.";
                return RedirectToAction(nameof(Details), new { id });
            }
            if (item.Table?.TableStatus is "Maintenance" or "WaitingPayment")
            {
                TempData["ErrorMessage"] = "Bàn đang bảo trì hoặc chờ thanh toán nên chưa thể nhận khách.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        var wasCheckedIn = item.ReservationStatus == "CheckedIn";
        item.ReservationStatus = status; item.UpdatedAt = DateTime.UtcNow;
        if (status == "CheckedIn")
        {
            item.CheckinTime = DateTime.UtcNow;
            if (item.Table != null) item.Table.TableStatus = "Occupied";
            if (openOrder is not null && openOrder.CustomerID is null) openOrder.CustomerID = item.CustomerID;
        }
        if (status is "Cancelled" or "Completed")
        {
            item.CheckoutTime = status == "Completed" ? DateTime.UtcNow : item.CheckoutTime;
            if (item.Table != null && !hasOpenOrder &&
                (item.Table.TableStatus == "Reserved" || (status == "Completed" && wasCheckedIn)))
                item.Table.TableStatus = "Available";
        }
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Could not change reservation {ReservationId} to {Status}.", id, status);
            TempData["ErrorMessage"] = "Không thể cập nhật trạng thái lịch đặt. Vui lòng thử lại.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (status == "Confirmed" && item.ReservationDate <= BusinessClock.Now.AddMinutes(-ReservationPolicy.DurationMinutes))
        {
            TempData["ErrorMessage"] = "Lịch đặt đã hết khung giờ phục vụ nên không thể xác nhận.";
            return RedirectToAction(nameof(Details), new { id });
        }
        TempData["SuccessMessage"] = $"Đã chuyển trạng thái sang {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadTablesAsync(int selected) => ViewBag.Tables = new SelectList(
        await context.RestaurantTables.AsNoTracking().Where(t => !t.IsDeleted && t.TableStatus != "Maintenance").OrderBy(t => t.TableNumber).ToListAsync(),
        "TableID", "TableNumber", selected);
    private bool IsAdmin() => HttpContext.Session.GetString("RoleName") == "Admin";
    private IActionResult RedirectToLogin() => RedirectToAction("Login", "Account", new { area = "" });
}
