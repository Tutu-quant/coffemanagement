using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Services;

/// <summary>
/// Handles reservation status transitions and auto-cancellation of overdue reservations.
/// All times use Asia/Ho_Chi_Minh (Vietnam) timezone via BusinessClock.
/// </summary>
public class ReservationStatusService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReservationStatusService> _logger;

    public ReservationStatusService(ApplicationDbContext context, ILogger<ReservationStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Auto-cancels reservations that are more than 30 minutes overdue and haven't been checked in.
    /// Should be called periodically (e.g., every 5 minutes) via a background service.
    /// </summary>
    public async Task<int> AutoCancelOverdueReservationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = BusinessClock.Now;
            var overdueThreshold = now.AddMinutes(-ReservationPolicy.HoldBeforeMinutes); // 30 minutes ago

            var overdueReservations = await _context.Reservations
                .Where(r => !r.IsDeleted &&
                           r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed" &&
                           r.ReservationDate <= overdueThreshold)
                .ToListAsync(cancellationToken);

            int cancelledCount = 0;
            foreach (var reservation in overdueReservations)
            {
                reservation.ReservationStatus = "Cancelled";
                reservation.UpdatedAt = DateTime.UtcNow;
                cancelledCount++;

                _logger.LogInformation(
                    "Auto-cancelled reservation {ReservationId} for table {TableId} - customer was 30+ minutes late",
                    reservation.ReservationID,
                    reservation.TableID);
            }

            if (cancelledCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Auto-cancelled {Count} overdue reservations", cancelledCount);
            }

            return cancelledCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-cancelling overdue reservations");
            return 0;
        }
    }

    /// <summary>
    /// Gets all reservations that are currently overdue (past reservation time but not yet 30 minutes late)
    /// </summary>
    public async Task<List<Models.Entities.Reservation>> GetOverdueReservationsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = BusinessClock.Now;
        var autoCancelThreshold = now.AddMinutes(-ReservationPolicy.HoldBeforeMinutes);

        return await _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted &&
                       (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed") &&
                       r.ReservationDate <= now &&
                       r.ReservationDate > autoCancelThreshold)
            .Include(r => r.Customer)
            .Include(r => r.Table)
            .OrderBy(r => r.ReservationDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets reservations coming up soon (within next 15 minutes)
    /// </summary>
    public async Task<List<Models.Entities.Reservation>> GetUpcomingReservationsAsync(
        int minutesWindow = 15,
        CancellationToken cancellationToken = default)
    {
        var now = BusinessClock.Now;
        var soonThreshold = now.AddMinutes(minutesWindow);

        return await _context.Reservations
            .AsNoTracking()
            .Where(r => !r.IsDeleted &&
                       (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed") &&
                       r.ReservationDate > now &&
                       r.ReservationDate <= soonThreshold)
            .Include(r => r.Customer)
            .Include(r => r.Table)
            .OrderBy(r => r.ReservationDate)
            .ToListAsync(cancellationToken);
    }
}
