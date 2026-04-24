using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Tags.Responses;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.RenameTag;

public sealed class RenameTagCommandHandler
    : IRequestHandler<RenameTagCommand, ErrorOr<TagResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public RenameTagCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<TagResponse>> Handle(
        RenameTagCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "RenameTag requires an authenticated user.");

        var id = new TagId(request.Id);
        var tag = await _dbContext.Tags
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (tag is null)
        {
            return TagErrors.NotFound(id);
        }

        var normalized = request.Name.Trim();
        var conflict = await _dbContext.Tags
            .AnyAsync(
                t => t.OwnerId == ownerId && t.Id != id && t.Name == normalized,
                cancellationToken);
        if (conflict)
        {
            return TagErrors.AlreadyExists(normalized);
        }

        var result = tag.Rename(request.Name);
        if (result.IsError)
        {
            return result.Errors;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TagResponse(tag.Id.Value, tag.Name, tag.CreatedAtUtc, 0);
    }
}
