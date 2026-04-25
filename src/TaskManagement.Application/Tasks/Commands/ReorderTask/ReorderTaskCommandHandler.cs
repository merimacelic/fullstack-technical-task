using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Ordering;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks.Commands.ReorderTask;

public sealed class ReorderTaskCommandHandler
    : IRequestHandler<ReorderTaskCommand, ErrorOr<TaskResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IOrderKeyService _orderKeyService;
    private readonly IReorderSerializer _reorderSerializer;

    public ReorderTaskCommandHandler(
        IApplicationDbContext dbContext,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        IOrderKeyService orderKeyService,
        IReorderSerializer reorderSerializer)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _orderKeyService = orderKeyService;
        _reorderSerializer = reorderSerializer;
    }

    public async ValueTask<ErrorOr<TaskResponse>> Handle(
        ReorderTaskCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "ReorderTask requires an authenticated user.");

        // Serialise per-owner: covers the full read-compute-write so two
        // concurrent reorders can't compute the same midpoint.
        using var ownerLock = await _reorderSerializer.AcquireAsync(ownerId, cancellationToken);

        var id = new TaskId(request.Id);
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == ownerId, cancellationToken);
        if (task is null)
        {
            return TaskErrors.NotFound(id);
        }

        var previousId = request.PreviousTaskId is { } prev ? new TaskId(prev) : (TaskId?)null;
        var nextId = request.NextTaskId is { } next ? new TaskId(next) : (TaskId?)null;

        var newKey = await _orderKeyService.BetweenAsync(ownerId, previousId, nextId, cancellationToken);
        if (newKey.IsError)
        {
            return newKey.Errors;
        }

        task.MoveTo(newKey.Value, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return task.ToResponse();
    }
}
