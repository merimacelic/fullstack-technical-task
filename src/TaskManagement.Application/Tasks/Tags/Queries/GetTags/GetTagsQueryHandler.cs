using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Tags.Responses;

namespace TaskManagement.Application.Tasks.Tags.Queries.GetTags;

public sealed class GetTagsQueryHandler
    : IRequestHandler<GetTagsQuery, ErrorOr<IReadOnlyList<TagResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetTagsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<IReadOnlyList<TagResponse>>> Handle(
        GetTagsQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "GetTags requires an authenticated user.");

        var tags = await _dbContext.Tags
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.CreatedAtUtc })
            .ToListAsync(cancellationToken);

        if (tags.Count == 0)
        {
            return Array.Empty<TagResponse>();
        }

        // Single round-trip instead of N+1: pull every task's tagId array for this
        // owner and group in memory. JSON SelectMany translation on SQL Server
        // (OPENJSON flatten) is brittle across EF versions, so the client-side
        // group is both simpler and portable. For the expected task volume per
        // user (100s, not millions), this is well below any cost threshold.
        var tagIdLists = await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId)
            .Select(t => EF.Property<List<Guid>>(t, "_tagIds"))
            .ToListAsync(cancellationToken);

        var counts = tagIdLists
            .SelectMany(ids => ids)
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        var responses = tags
            .Select(t => new TagResponse(
                t.Id.Value,
                t.Name,
                t.CreatedAtUtc,
                counts.TryGetValue(t.Id.Value, out var count) ? count : 0))
            .ToList();

        return responses;
    }
}
