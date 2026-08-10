using Microsoft.AspNetCore.SignalR;

namespace Quản_lý_quán_cafe.Realtime;

public record EntityChange(string EntityType, int EntityId, string ChangeType);

public interface IRealtimeUpdateNotifier
{
    Task NotifyAsync(IReadOnlyCollection<EntityChange> changes, CancellationToken cancellationToken = default);
}

public class RealtimeUpdateNotifier(
    IHubContext<AppStateHub> hubContext,
    ILogger<RealtimeUpdateNotifier> logger) : IRealtimeUpdateNotifier
{
    public async Task NotifyAsync(IReadOnlyCollection<EntityChange> changes, CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0) return;

            var payload = new
            {
                entityTypes = changes.Select(x => x.EntityType).Distinct().ToArray(),
                changes,
                occurredAt = DateTimeOffset.Now
            };

        try
        {
            await hubContext.Clients.All.SendAsync("StateChanged", payload, cancellationToken);
        }
        catch (Exception exception)
        {

            logger.LogWarning(exception, "Could not broadcast {Count} realtime state changes.", changes.Count);
        }
    }
}
