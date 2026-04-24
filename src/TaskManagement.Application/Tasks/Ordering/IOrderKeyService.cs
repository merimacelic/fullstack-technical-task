using ErrorOr;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Ordering;

// Seam used by task handlers to compute OrderKey values. Registered in DI so
// handlers don't `new` up infrastructure-touching services.
//
// Implementations must mutate tracked entities without saving — callers own the
// SaveChangesAsync so the whole reorder commits (or rolls back) atomically.
public interface IOrderKeyService
{
    Task<decimal> NextForOwnerAsync(Guid ownerId, CancellationToken ct);

    Task<ErrorOr<decimal>> BetweenAsync(
        Guid ownerId,
        TaskId? previousId,
        TaskId? nextId,
        CancellationToken ct);

    Task<IReadOnlyList<TaskItem>> RebalanceAsync(Guid ownerId, CancellationToken ct);
}
