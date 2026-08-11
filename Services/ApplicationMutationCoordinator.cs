namespace Quản_lý_quán_cafe.Services;

/// <summary>
/// Serializes short business mutations that must recheck table, reservation,
/// order, payment and inventory state together when the application uses SQLite.
/// </summary>
public interface IApplicationMutationCoordinator
{
    ValueTask<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default);
}

public sealed class ApplicationMutationCoordinator : IApplicationMutationCoordinator, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async ValueTask<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        return new Releaser(_gate);
    }

    public void Dispose() => _gate.Dispose();

    private sealed class Releaser(SemaphoreSlim gate) : IAsyncDisposable
    {
        private SemaphoreSlim? _gate = gate;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _gate, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
