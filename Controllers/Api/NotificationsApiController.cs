using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/notifications")]
[SessionAuthorize("Admin,Cashier")]
public class NotificationsApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("reservations/pending-count")]
    public async Task<IActionResult> GetPendingReservationCount(CancellationToken cancellationToken)
    {
        var pendingReservations = await context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                !reservation.IsDeleted &&
                reservation.ReservationStatus == "Pending",
                cancellationToken);

        return Ok(new { pendingReservations });
    }
}
