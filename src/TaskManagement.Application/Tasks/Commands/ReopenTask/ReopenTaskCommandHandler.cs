using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.ReopenTask;

public sealed class ReopenTaskCommandHandler : IRequestHandler<ReopenTaskCommand, ErrorOr<TaskResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public ReopenTaskCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<TaskResponse>> Handle(
        ReopenTaskCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "ReopenTask requires an authenticated user.");

        var id = new TaskId(request.Id);
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        var result = task.Reopen(_clock.UtcNow);
        if (result.IsError)
        {
            return result.Errors;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}
