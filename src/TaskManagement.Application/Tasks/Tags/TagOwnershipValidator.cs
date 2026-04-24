using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags;

// Shared helper — both CreateTask and UpdateTask need to confirm every supplied
// tag id belongs to the acting user. Extracted so the two handlers can't drift.
internal static class TagOwnershipValidator
{
    public static async Task<ErrorOr<Success>> EnsureOwnedAsync(
        IApplicationDbContext db,
        Guid ownerId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken ct)
    {
        if (tagIds.Count == 0)
        {
            return Result.Success;
        }

        var distinctIds = tagIds.Distinct().Select(g => new TagId(g)).ToList();
        var ownedGuids = await db.Tags
            .Where(t => t.OwnerId == ownerId && distinctIds.Contains(t.Id))
            .Select(t => t.Id.Value)
            .ToListAsync(ct);

        var missing = distinctIds
            .Select(t => t.Value)
            .Except(ownedGuids)
            .Select(g => new TagId(g))
            .ToList();

        if (missing.Count == 0)
        {
            return Result.Success;
        }

        // Surface the whole set so the caller fixes all mismatched ids in one
        // round-trip. Single vs. many uses distinct error factories so the
        // existing single-tag path keeps its familiar error shape.
        return missing.Count == 1
            ? TagErrors.NotFound(missing[0])
            : TagErrors.NotFoundMany(missing);
    }
}
