using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Customer.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using TableStatuses = Quản_lý_quán_cafe.Models.Enums.TableStatus;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
public class ReservationsController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var customer = await GetOrCreateCustomerAsync();
        var items = await context.Reservations.AsNoTracking().Include(r => r.Table)
            .Where(r => r.CustomerID == customer.CustomerID && !r.IsDeleted)
            .OrderByDescending(r => r.ReservationDate).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var model = new ReservationViewModel();
        await LoadTablesAsync(model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservationViewModel model)
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var table = await context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == model.TableID && !t.IsDeleted);
        if (table is null || table.TableStatus == TableStatuses.Maintenance)
            ModelState.AddModelError(nameof(model.TableID), "Bàn không tồn tại hoặc không hỗ trợ đặt trước.");
        else if (model.NumberOfGuests > table.Capacity)
            ModelState.AddModelError(nameof(model.NumberOfGuests), $"Bàn {table.TableNumber} chỉ phục vụ tối đa {table.Capacity} khách.");
        if (model.ReservationDate <= DateTime.Now)
            ModelState.AddModelError(nameof(model.ReservationDate), "Thời gian đặt phải ở tương lai.");

        if (table is not null)
        {
            var start = model.ReservationDate.AddHours(-2);
            var end = model.ReservationDate.AddHours(2);
            var conflict = await context.Reservations.AnyAsync(r => !r.IsDeleted && r.TableID == model.TableID &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
                r.ReservationDate > start && r.ReservationDate < end);
            if (conflict) ModelState.AddModelError(nameof(model.TableID), "Bàn đã được đặt gần khung giờ này.");
        }

        if (!ModelState.IsValid)
        {
            await LoadTablesAsync(model);
            return View(model);
        }

        var customer = await GetOrCreateCustomerAsync();
        context.Reservations.Add(new Reservation
        {
            CustomerID = customer.CustomerID,
            TableID = model.TableID,
            ReservationDate = model.ReservationDate,
            NumberOfGuests = model.NumberOfGuests,
            Notes = model.Notes,
            ReservationStatus = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đặt bàn thành công. Vui lòng chờ quán xác nhận.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!IsLoggedIn()) return RedirectToLogin();
        var customer = await GetOrCreateCustomerAsync();
        var item = await context.Reservations.FirstOrDefaultAsync(r => r.ReservationID == id && r.CustomerID == customer.CustomerID && !r.IsDeleted);
        if (item is null) return NotFound();
        if (item.ReservationStatus is not ("Pending" or "Confirmed"))
        {
            TempData["ErrorMessage"] = "Không thể hủy đặt bàn ở trạng thái hiện tại.";
            return RedirectToAction(nameof(Index));
        }
        item.ReservationStatus = "Cancelled";
        item.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã hủy đặt bàn.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTablesAsync(ReservationViewModel model)
    {
        model.Tables = await context.RestaurantTables.AsNoTracking()
            .Where(t => !t.IsDeleted && t.TableStatus != TableStatuses.Maintenance)
            .OrderBy(t => t.TableNumber)
            .Select(t => new SelectListItem($"{t.TableNumber} - {t.Capacity} khách - {t.Location}", t.TableID.ToString()))
            .ToListAsync();
    }

    private bool IsLoggedIn() => (HttpContext.Session.GetInt32("UserId") ?? 0) > 0;
    private IActionResult RedirectToLogin() => RedirectToAction("Login", "Account", new { area = "" });

    private async Task<Models.Entities.Customer> GetOrCreateCustomerAsync()
    {
        var username = HttpContext.Session.GetString("Username") ?? "customer";
        var email = $"{username}@local.cafe";
        var customer = await context.Customers.FirstOrDefaultAsync(c => !c.IsDeleted && c.Email == email);
        if (customer is not null) return customer;
        customer = new Models.Entities.Customer
        {
            CustomerName = HttpContext.Session.GetString("FullName") ?? username,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer;
    }
}
