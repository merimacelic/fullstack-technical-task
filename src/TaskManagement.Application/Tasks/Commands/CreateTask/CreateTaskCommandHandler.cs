using ErrorOr;
using Mediator;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Ordering;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Application.Tasks.Tags;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, ErrorOr<TaskResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderKeyService _orderKeyService;

    public CreateTaskCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        IOrderKeyService orderKeyService)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _orderKeyService = orderKeyService;
    }

    public async ValueTask<ErrorOr<TaskResponse>> Handle(
        CreateTaskCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "CreateTask requires an authenticated user. Ensure the endpoint is protected with RequireAuthorization().");

        var priority = TaskPriority.FromName(request.Priority);

        var orderKey = await _orderKeyService.NextForOwnerAsync(ownerId, cancellationToken);

        var createResult = TaskItem.Create(
            ownerId,
            request.Title,
            request.Description,
            priority,
            request.DueDateUtc,
            _clock.UtcNow,
            orderKey);

        if (createResult.IsError)
        {
            return createResult.Errors;
        }

        var task = createResult.Value;

        if (request.TagIds is { Count: > 0 })
        {
            var validation = await TagOwnershipValidator.EnsureOwnedAsync(
                _dbContext, ownerId, request.TagIds, cancellationToken);
            if (validation.IsError)
            {
                return validation.Errors;
            }

            task.ReplaceTags(request.TagIds.Select(t => new TagId(t)), _clock.UtcNow);
        }

        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }
}
