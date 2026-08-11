using System.Collections.Concurrent;

namespace Quản_lý_quán_cafe.Realtime;

public interface IStaffNotificationConnectionRegistry
{
    void Register(string connectionId, int userId);
    void Unregister(string connectionId);
    IReadOnlyCollection<string> GetConnections(IReadOnlySet<int> activeStaffUserIds);
}

public sealed class StaffNotificationConnectionRegistry : IStaffNotificationConnectionRegistry
{
    private readonly ConcurrentDictionary<string, int> _connections = new();

    public void Register(string connectionId, int userId)
    {
        if (!string.IsNullOrWhiteSpace(connectionId) && userId > 0)
            _connections[connectionId] = userId;
    }

    public void Unregister(string connectionId)
    {
        if (!string.IsNullOrWhiteSpace(connectionId))
            _connections.TryRemove(connectionId, out _);
    }

    public IReadOnlyCollection<string> GetConnections(IReadOnlySet<int> activeStaffUserIds) =>
        _connections
            .Where(connection => activeStaffUserIds.Contains(connection.Value))
            .Select(connection => connection.Key)
            .ToArray();
}
