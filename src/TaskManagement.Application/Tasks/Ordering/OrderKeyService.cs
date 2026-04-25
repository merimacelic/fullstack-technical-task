using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Ordering;

// Pure-Application helper that computes OrderKey values for new and reordered tasks.
// Kept out of the domain because it needs to query existing keys.
//
// Transactional contract: RebalanceAsync mutates tracked entities but does NOT
// save. BetweenAsync composes with RebalanceAsync and likewise leaves persistence
// to the caller — so a reorder + rebalance reaches the database as a single
// SaveChanges (the caller's), and a failure rolls back both the rebalance and
// the move. See ReorderTaskCommandHandler.
internal sealed class OrderKeyService : IOrderKeyService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public OrderKeyService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    // Next key for a new task — append to the bottom of the user's list.
    public async Task<decimal> NextForOwnerAsync(Guid ownerId, CancellationToken ct)
    {
        var max = await _db.Tasks
            .Where(t => t.OwnerId == ownerId)
            .Select(t => (decimal?)t.OrderKey)
            .MaxAsync(ct);
        return (max ?? 0m) + TaskItem.OrderKeyStep;
    }

    // Compute a new OrderKey for a task dropped between two neighbours. Neighbour IDs
    // may be null (drop at start or end). Returns NotFound if a supplied neighbour id
    // does not belong to the owner. If the midpoint gap collapses below the rebalance
    // threshold, the whole list is renumbered first and the midpoint is recomputed
    // against the rebalanced keys — so the returned value is always well-separated
    // from its neighbours.
    public async Task<ErrorOr<decimal>> BetweenAsync(
        Guid ownerId,
        TaskId movingId,
        TaskId? previousId,
        TaskId? nextId,
        CancellationToken ct)
    {
        var prevResult = await LoadNeighbourKeyAsync(ownerId, previousId, ct);
        if (prevResult.IsError)
        {
            return prevResult.Errors;
        }

        var nextResult = await LoadNeighbourKeyAsync(ownerId, nextId, ct);
        if (nextResult.IsError)
        {
            return nextResult.Errors;
        }

        var (prev, next) = await ResolveBoundsAsync(ownerId, movingId, prevResult.Value, nextResult.Value, ct);

        if (next <= prev)
        {
            // Defensive: neighbours out of order — rebalance and drop at the end.
            var rebalanced = await RebalanceAsync(ownerId, ct);
            return rebalanced.Count == 0
                ? TaskItem.OrderKeyStep
                : rebalanced[^1].OrderKey + TaskItem.OrderKeyStep;
        }

        var gap = next - prev;
        if (gap >= TaskItem.OrderKeyRebalanceThreshold)
        {
            return prev + (gap / 2m);
        }

        // Gap collapsed — renumber everyone, then recompute the midpoint from
        // the in-memory rebalanced list. We can't re-query the DB here because
        // RebalanceAsync mutates tracked entities without saving (saving is
        // the caller's job, so the whole reorder commits atomically).
        var fresh = await RebalanceAsync(ownerId, ct);
        return MidpointFromList(fresh, movingId, previousId, nextId);
    }

    // Given the user-supplied prev/next hints, return the actual gap bounds in
    // the database right now — i.e. the smallest existing OrderKey strictly
    // greater than prev among the owner's other tasks, capped at nextHint if
    // the caller supplied one. This is what makes serialised concurrent
    // reorders into the same user-visible gap pick distinct midpoints: the
    // second one sees the first one's insertion sitting between the hints and
    // narrows the gap accordingly.
    private async Task<(decimal Prev, decimal Next)> ResolveBoundsAsync(
        Guid ownerId,
        TaskId movingId,
        decimal? prevHint,
        decimal? nextHint,
        CancellationToken ct)
    {
        var prev = prevHint ?? 0m;

        var actualNext = await _db.Tasks
            .Where(t => t.OwnerId == ownerId
                && t.Id != movingId
                && t.OrderKey > prev
                && (nextHint == null || t.OrderKey <= nextHint))
            .OrderBy(t => t.OrderKey)
            .Select(t => (decimal?)t.OrderKey)
            .FirstOrDefaultAsync(ct);

        var next = (actualNext, nextHint) switch
        {
            (not null, not null) => Math.Min(actualNext.Value, nextHint.Value),
            (not null, null) => actualNext.Value,
            (null, not null) => nextHint.Value,
            (null, null) => prev + (TaskItem.OrderKeyStep * 2m),
        };

        return (prev, next);
    }

    // In-memory equivalent of ResolveBoundsAsync, used after a rebalance when
    // tracked entities have been mutated but not yet persisted.
    private static decimal MidpointFromList(
        IReadOnlyList<TaskItem> tasks,
        TaskId movingId,
        TaskId? previousId,
        TaskId? nextId)
    {
        var prevHint = previousId is { } p ? tasks.FirstOrDefault(t => t.Id == p)?.OrderKey : null;
        var nextHint = nextId is { } n ? tasks.FirstOrDefault(t => t.Id == n)?.OrderKey : null;
        var prev = prevHint ?? 0m;

        var actualNext = tasks
            .Where(t => t.Id != movingId
                && t.OrderKey > prev
                && (nextHint == null || t.OrderKey <= nextHint))
            .OrderBy(t => t.OrderKey)
            .Select(t => (decimal?)t.OrderKey)
            .FirstOrDefault();

        var next = (actualNext, nextHint) switch
        {
            (not null, not null) => Math.Min(actualNext.Value, nextHint.Value),
            (not null, null) => actualNext.Value,
            (null, not null) => nextHint.Value,
            (null, null) => prev + (TaskItem.OrderKeyStep * 2m),
        };

        return prev + ((next - prev) / 2m);
    }

    // Renumber all of a user's tasks in their current order with OrderKeyStep
    // spacing. Mutates tracked entities only — the caller is responsible for
    // SaveChangesAsync, so a single commit reaches the database for the whole
    // operation. Returns the rebalanced list so neighbour keys can be read back
    // without a second round-trip to an unsaved projection.
    public async Task<IReadOnlyList<TaskItem>> RebalanceAsync(Guid ownerId, CancellationToken ct)
    {
        var tasks = await _db.Tasks
            .Where(t => t.OwnerId == ownerId)
            .OrderBy(t => t.OrderKey)
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        var key = TaskItem.OrderKeyStep;
        foreach (var task in tasks)
        {
            task.MoveTo(key, now);
            key += TaskItem.OrderKeyStep;
        }

        return tasks;
    }

    private async Task<ErrorOr<decimal?>> LoadNeighbourKeyAsync(
        Guid ownerId,
        TaskId? neighbourId,
        CancellationToken ct)
    {
        if (neighbourId is null)
        {
            return (decimal?)null;
        }

        var id = neighbourId.Value;
        var key = await _db.Tasks
            .Where(t => t.Id == id && t.OwnerId == ownerId)
            .Select(t => (decimal?)t.OrderKey)
            .FirstOrDefaultAsync(ct);

        if (key is null)
        {
            return TaskErrors.NotFound(id);
        }

        return key;
    }
}
