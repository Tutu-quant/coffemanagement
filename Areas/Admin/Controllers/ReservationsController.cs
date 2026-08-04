using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers;

[Area("Admin")]
public class ReservationsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? status, DateTime? date)
    {
        if (!IsAdmin()) return RedirectToLogin();
        var query = context.Reservations.AsNoTracking().Include(r => r.Customer).Include(r => r.Table).Where(r => !r.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.ReservationStatus == status);
        if (date.HasValue) query = query.Where(r => r.ReservationDate.Date == date.Value.Date);
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
        await LoadTablesAsync(item.TableID);
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ReservationID,TableID,ReservationDate,NumberOfGuests,Notes,ReservationStatus")] Reservation model)
    {
        if (!IsAdmin()) return Forbid();
        if (id != model.ReservationID) return NotFound();
        var item = await context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        if (item is null) return NotFound();
        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == model.TableID && !t.IsDeleted);
        if (table is null || model.NumberOfGuests < 1 || model.NumberOfGuests > table.Capacity)
            ModelState.AddModelError(nameof(model.NumberOfGuests), "Số khách không phù hợp sức chứa bàn.");
        var conflict = await context.Reservations.AnyAsync(r => r.ReservationID != id && !r.IsDeleted && r.TableID == model.TableID &&
            r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
            r.ReservationDate > model.ReservationDate.AddHours(-2) && r.ReservationDate < model.ReservationDate.AddHours(2));
        if (conflict) ModelState.AddModelError(nameof(model.TableID), "Bàn đã có lịch đặt gần thời gian này.");
        if (!ModelState.IsValid) { await LoadTablesAsync(model.TableID); return View(model); }
        item.TableID = model.TableID; item.ReservationDate = model.ReservationDate; item.NumberOfGuests = model.NumberOfGuests;
        item.Notes = model.Notes?.Trim(); item.ReservationStatus = model.ReservationStatus; item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
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
        var item = await context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        if (item is null) return NotFound();
        item.ReservationStatus = status; item.UpdatedAt = DateTime.UtcNow;
        if (status == "CheckedIn") { item.CheckinTime = DateTime.UtcNow; if (item.Table != null) item.Table.TableStatus = "Occupied"; }
        if (status is "Cancelled" or "Completed")
        {
            item.CheckoutTime = status == "Completed" ? DateTime.UtcNow : item.CheckoutTime;
            if (item.Table != null && (status == "Completed" || item.Table.TableStatus == "Reserved"))
                item.Table.TableStatus = "Available";
        }
        if (status == "Confirmed" && item.Table?.TableStatus == "Available") item.Table.TableStatus = "Reserved";
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã chuyển trạng thái sang {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadTablesAsync(int selected) => ViewBag.Tables = new SelectList(
        await context.RestaurantTables.AsNoTracking().Where(t => !t.IsDeleted && t.TableStatus != "Maintenance").OrderBy(t => t.TableNumber).ToListAsync(),
        "TableID", "TableNumber", selected);
    private bool IsAdmin() => HttpContext.Session.GetString("RoleName") == "Admin";
    private IActionResult RedirectToLogin() => RedirectToAction("Login", "Account", new { area = "" });
}
