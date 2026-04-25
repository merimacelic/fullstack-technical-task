namespace TaskManagement.Application.Tasks.Ordering;

// Per-owner mutex for the reorder pipeline. Process-local — swap for a
// distributed lock if we ever scale beyond a single replica.
public interface IReorderSerializer
{
    Task<IDisposable> AcquireAsync(Guid ownerId, CancellationToken cancellationToken);
}
