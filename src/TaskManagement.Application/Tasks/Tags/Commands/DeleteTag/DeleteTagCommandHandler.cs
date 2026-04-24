using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.DeleteTag;

public sealed class DeleteTagCommandHandler
    : IRequestHandler<DeleteTagCommand, ErrorOr<Deleted>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public DeleteTagCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Deleted>> Handle(
        DeleteTagCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "DeleteTag requires an authenticated user.");

        var id = new TagId(request.Id);
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (tag is null)
        {
            return TagErrors.NotFound(id);
        }

        // Sweep the tag association out of every task that references it. The
        // Tasks.TagIds JSON column is a shadow field; access it via EF.Property so
        // the filter translates to SQL (OPENJSON contains on SQL Server).
        var tagValue = id.Value;
        var tasksWithTag = await _dbContext.Tasks
            .Where(t => t.OwnerId == ownerId
                        && EF.Property<List<Guid>>(t, "_tagIds").Contains(tagValue))
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        foreach (var task in tasksWithTag)
        {
            task.RemoveTag(id, now);
        }

        _dbContext.Tags.Remove(tag);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Close the read-then-save race: if another request stamped this tag onto
        // a task between our read above and the save, that row would still carry
        // the dangling tag id. Sweep one more time via a bulk update so the final
        // state has zero references to the deleted tag no matter the interleaving.
        var stragglers = await _dbContext.Tasks
            .Where(t => t.OwnerId == ownerId
                        && EF.Property<List<Guid>>(t, "_tagIds").Contains(tagValue))
            .ToListAsync(cancellationToken);

        if (stragglers.Count > 0)
        {
            foreach (var task in stragglers)
            {
                task.RemoveTag(id, now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Deleted;
    }
}
