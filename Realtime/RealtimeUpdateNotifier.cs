using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;

namespace Quản_lý_quán_cafe.Realtime;

public record EntityChange(string EntityType, int EntityId, string ChangeType);

public interface IRealtimeUpdateNotifier
{
    Task NotifyAsync(IReadOnlyCollection<EntityChange> changes, CancellationToken cancellationToken = default);
}

public class RealtimeUpdateNotifier(
    IHubContext<AppStateHub> hubContext,
    IServiceScopeFactory scopeFactory,
    IStaffNotificationConnectionRegistry staffConnections,
    ILogger<RealtimeUpdateNotifier> logger) : IRealtimeUpdateNotifier
{
    public async Task NotifyAsync(IReadOnlyCollection<EntityChange> changes, CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0) return;

        var occurredAt = DateTimeOffset.Now;
        var payload = new
        {
            entityTypes = changes.Select(x => x.EntityType).Distinct().ToArray(),
            changes,
            occurredAt
        };

        try
        {
            await hubContext.Clients.All.SendAsync("StateChanged", payload, cancellationToken);
        }
        catch (Exception exception)
        {

            logger.LogWarning(exception, "Could not broadcast {Count} realtime state changes.", changes.Count);
        }

        var createdReservationIds = changes
            .Where(change => change.EntityType == "Reservation" && change.ChangeType == "Added")
            .Select(change => change.EntityId)
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (createdReservationIds.Length == 0) return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var activeStaffUserIds = (await context.Users
                .AsNoTracking()
                .Where(user => !user.IsDeleted && user.IsActive &&
                    user.Role != null && !user.Role.IsDeleted &&
                    (user.Role.RoleName == "Admin" || user.Role.RoleName == "Cashier") &&
                    user.Employee != null && !user.Employee.IsDeleted && user.Employee.IsActive)
                .Select(user => user.UserID)
                .ToListAsync(cancellationToken))
                .ToHashSet();
            var activeConnections = staffConnections.GetConnections(activeStaffUserIds);
            if (activeConnections.Count == 0) return;

            await hubContext.Clients.Clients(activeConnections)
                .SendAsync("ReservationCreated", new { reservationIds = createdReservationIds, occurredAt }, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not broadcast {Count} new reservation notifications.", createdReservationIds.Length);
        }
    }
}
