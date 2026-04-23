using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Tasks.Mapping;
using TaskManagement.Application.Tasks.Responses;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Application.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, ErrorOr<PagedResult<TaskResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetTasksQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<TaskResponse>>> Handle(
        GetTasksQuery request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUser.UserId ?? throw new InvalidOperationException(
            "GetTasks requires an authenticated user.");

        if (!string.IsNullOrWhiteSpace(request.Status) && !TaskItemStatus.TryFromName(request.Status, out _))
        {
            return TaskErrors.UnknownStatus(request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority) && !TaskPriority.TryFromName(request.Priority, out _))
        {
            return TaskErrors.UnknownPriority(request.Priority);
        }

        var pagination = new PaginationRequest(request.Page, request.PageSize);

        var query = _dbContext.Tasks.AsNoTracking().Where(t => t.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = TaskItemStatus.FromName(request.Status);
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = TaskPriority.FromName(request.Priority);
            query = query.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // LIKE metacharacters from user input become literal matches. Without
            // this, typing `50%` would match every task whose title starts with 50.
            var term = EscapeLikePattern(request.Search.Trim());
            var pattern = $"%{term}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Title, pattern, "\\") ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, "\\")));
        }

        if (request.TagId is { } tagGuid)
        {
            query = query.Where(t => EF.Property<List<Guid>>(t, "_tagIds").Contains(tagGuid));
        }

        query = ApplySort(query, request.SortBy, request.SortDirection);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var responses = items.Select(t => t.ToResponse()).ToList();
        return new PagedResult<TaskResponse>(
            responses,
            pagination.NormalizedPage,
            pagination.NormalizedPageSize,
            total);
    }

    // SQL Server LIKE metacharacters: %, _, [, ]. Escaped with a backslash so the
    // EF.Functions.Like call above treats them as literals.
    private static string EscapeLikePattern(string term) =>
        term
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal);

    private static IQueryable<TaskItem> ApplySort(
        IQueryable<TaskItem> query,
        TaskSortBy sortBy,
        SortDirection direction)
    {
        // Every primary sort resolves ties by Id so pagination is deterministic
        // across requests — without this, two tasks with equal Priority/Title
        // can swap positions between page fetches and a row appears twice or
        // not at all.
        var desc = direction == SortDirection.Descending;
        IOrderedQueryable<TaskItem> primary = sortBy switch
        {
            TaskSortBy.Title => desc
                ? query.OrderByDescending(t => t.Title)
                : query.OrderBy(t => t.Title),
            TaskSortBy.DueDate => desc
                ? query.OrderByDescending(t => t.DueDateUtc)
                : query.OrderBy(t => t.DueDateUtc),
            TaskSortBy.Priority => desc
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            TaskSortBy.UpdatedAt => desc
                ? query.OrderByDescending(t => t.UpdatedAtUtc)
                : query.OrderBy(t => t.UpdatedAtUtc),
            TaskSortBy.Order => desc
                ? query.OrderByDescending(t => t.OrderKey)
                : query.OrderBy(t => t.OrderKey),
            _ => desc
                ? query.OrderByDescending(t => t.CreatedAtUtc)
                : query.OrderBy(t => t.CreatedAtUtc),
        };

        return primary.ThenBy(t => t.Id);
    }
}
