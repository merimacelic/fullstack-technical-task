using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Application.Tasks.Tags;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ErrorOr<TaskResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public UpdateTaskCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<TaskResponse>> Handle(
        UpdateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "UpdateTask requires an authenticated user.");

        var id = new TaskId(request.Id);
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        var priority = TaskPriority.FromName(request.Priority);

        var result = task.UpdateDetails(
            request.Title,
            request.Description,
            priority,
            request.DueDateUtc,
            _clock.UtcNow);

        if (result.IsError)
        {
            return result.Errors;
        }

        // TagIds semantics: null means "leave tags untouched"; an empty array means
        // "clear all tags". The endpoint and OpenAPI summary both document this.
        if (request.TagIds is not null)
        {
            var validation = await TagOwnershipValidator.EnsureOwnedAsync(
                _dbContext, ownerId, request.TagIds, cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            task.ReplaceTags(request.TagIds.Select(t => new TagId(t)), _clock.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}
