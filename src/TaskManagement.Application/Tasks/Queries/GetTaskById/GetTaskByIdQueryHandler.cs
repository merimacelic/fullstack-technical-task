using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, ErrorOr<TaskResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetTaskByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<TaskResponse>> Handle(
        GetTaskByIdQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "GetTaskById requires an authenticated user.");

        var id = new TaskId(request.Id);
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);

        return task is null
            ? TaskErrors.NotFound(id)
            : task.ToResponse();
    }
}
