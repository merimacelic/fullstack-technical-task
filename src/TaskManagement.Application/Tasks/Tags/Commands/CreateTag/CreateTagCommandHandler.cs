using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Tags.Responses;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Tags.Commands.CreateTag;

public sealed class CreateTagCommandHandler
    : IRequestHandler<CreateTagCommand, ErrorOr<TagResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public CreateTagCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<TagResponse>> Handle(
        CreateTagCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "CreateTag requires an authenticated user.");

        var normalized = request.Name.Trim();

        // Pre-check keeps the happy path a clean 409; the unique index on
        // (OwnerId, Name) is still the source of truth — under concurrency two
        // clients can both pass the check and only one will commit. The
        // DbUpdateException handler below translates that race to the same 409
        // a sequential caller would have seen, instead of leaking a 500.
        var exists = await _dbContext.Tags
            .AnyAsync(t => t.OwnerId == ownerId && t.Name == normalized, cancellationToken);
        if (exists)
        {
            return TagErrors.AlreadyExists(normalized);
        }

        var result = Tag.Create(ownerId, request.Name, _clock.UtcNow);
        if (result.IsError)
        {
            return result.Errors;
        }

        var tag = result.Value;
        _dbContext.Tags.Add(tag);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Re-check by name to distinguish a race on the (OwnerId, Name) unique
            // index from unrelated persistence failures. Stays portable — no raw
            // SqlException reference leaking into Application.
            var nowExists = await _dbContext.Tags
                .AnyAsync(t => t.OwnerId == ownerId && t.Name == normalized, cancellationToken);
            if (nowExists)
            {
                return TagErrors.AlreadyExists(normalized);
            }

            throw;
        }

        return new TagResponse(tag.Id.Value, tag.Name, tag.CreatedAtUtc, 0);
    }
}
