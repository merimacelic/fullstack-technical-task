using System.Collections.Concurrent;

namespace TaskManagement.Application.Tasks.Ordering;

internal sealed class PerOwnerReorderSerializer : IReorderSerializer
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _gates = new();

    public async Task<IDisposable> AcquireAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var gate = _gates.GetOrAdd(ownerId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(gate);
    }

    private sealed class Releaser : IDisposable
    {
        private SemaphoreSlim? _gate;

        public Releaser(SemaphoreSlim gate) => _gate = gate;

        // Guarded against double-dispose so a wrapping `using` can't double-release.
        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}
