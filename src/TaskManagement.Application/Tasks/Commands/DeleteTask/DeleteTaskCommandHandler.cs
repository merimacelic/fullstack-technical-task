using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.DeleteTask;

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, ErrorOr<Deleted>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteTaskCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Deleted>> Handle(
        DeleteTaskCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "DeleteTask requires an authenticated user.");

        var id = new TaskId(request.Id);
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Deleted;
    }
}
