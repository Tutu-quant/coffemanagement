using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using TableStatuses = Quản_lý_quán_cafe.Models.Enums.TableStatus;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Controllers.Api;

[ApiController, Route("api/tables")]
public class TablesApiController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> Available([FromQuery] DateTime at, [FromQuery] int guests = 1)
    {
        if (at <= BusinessClock.Now || guests is < 1 or > 50) return BadRequest(new { message = "Thời gian hoặc số khách không hợp lệ." });
        var start = at.AddHours(-2); var end = at.AddHours(2);
        var busy = context.Reservations.Where(r => !r.IsDeleted && r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" && r.ReservationDate > start && r.ReservationDate < end).Select(r => r.TableID);
        var currentUseCutoff = BusinessClock.Now.AddMinutes(ReservationPolicy.DurationMinutes);
        return Ok(await context.RestaurantTables.AsNoTracking().Where(t => !t.IsDeleted &&
                t.TableStatus != TableStatuses.Maintenance &&
                (at > currentUseCutoff || (t.TableStatus != TableStatuses.Occupied && t.TableStatus != TableStatuses.WaitingPayment)) &&
                t.Capacity >= guests && !busy.Contains(t.TableID))
            .OrderBy(t => t.Capacity).Select(t => new { t.TableID, t.TableNumber, t.Capacity, t.Location, t.TableStatus }).ToListAsync());
    }
}
