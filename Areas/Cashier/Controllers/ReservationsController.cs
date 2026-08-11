using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers;

[Area("Cashier")]
[SessionAuthorize("Cashier,Admin")]
public class ReservationsController(ApplicationDbContext context) : Controller
{
    private static readonly string[] ValidStatuses =
        ["Pending", "Confirmed", "CheckedIn", "Completed", "Cancelled"];

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        status = ValidStatuses.Contains(status, StringComparer.Ordinal) ? status : null;
        pageNumber = Math.Max(1, pageNumber);
        const int pageSize = 20;

        var query = context.Reservations
            .AsNoTracking()
            .Where(reservation => !reservation.IsDeleted);

        if (status is not null)
            query = query.Where(reservation => reservation.ReservationStatus == status);

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)pageSize));
        pageNumber = Math.Min(pageNumber, totalPages);

        var reservations = await query
            .Include(reservation => reservation.Customer)
            .Include(reservation => reservation.Table)
            .OrderBy(reservation => reservation.ReservationStatus == "Pending" ? 0 :
                reservation.ReservationStatus == "Confirmed" ? 1 :
                reservation.ReservationStatus == "CheckedIn" ? 2 : 3)
            .ThenBy(reservation => reservation.ReservationDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        ViewBag.Status = status;
        ViewBag.PageNumber = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        return View(reservations);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var reservation = await context.Reservations
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Table)
            .FirstOrDefaultAsync(item => item.ReservationID == id && !item.IsDeleted, cancellationToken);

        return reservation is null ? NotFound() : View(reservation);
    }
}
