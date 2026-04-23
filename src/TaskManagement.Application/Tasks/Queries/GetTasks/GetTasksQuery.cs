using ErrorOr;
using Mediator;
using TaskManagement.Application.Common.Pagination;
using TaskManagement.Application.Tasks.Responses;

namespace TaskManagement.Application.Tasks.Queries.GetTasks;

/// <summary>Query-string parameters for <c>GET /api/tasks</c>.</summary>
/// <param name="Status">Optional status filter: <c>Pending</c>, <c>InProgress</c>, or <c>Completed</c>.</param>
/// <param name="Priority">Optional priority filter: <c>Low</c>, <c>Medium</c>, <c>High</c>, or <c>Critical</c>.</param>
/// <param name="Search">Optional substring match against title and description. LIKE wildcards are escaped.</param>
/// <param name="TagId">Optional tag filter; only tasks carrying this tag id are returned.</param>
/// <param name="SortBy">Sort key. Defaults to <see cref="TaskSortBy.CreatedAt"/>.</param>
/// <param name="SortDirection">Sort direction. Defaults to <see cref="SortDirection.Descending"/>.</param>
/// <param name="Page">1-based page number; clamped to a sensible minimum server-side.</param>
/// <param name="PageSize">Items per page; clamped server-side to avoid unbounded reads.</param>
public sealed record GetTasksQuery(
    string? Status = null,
    string? Priority = null,
    string? Search = null,
    Guid? TagId = null,
    TaskSortBy SortBy = TaskSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Descending,
    int Page = 1,
    int PageSize = 20) : IRequest<ErrorOr<PagedResult<TaskResponse>>>;

/// <summary>Fields the task list endpoint can sort on.</summary>
public enum TaskSortBy
{
    /// <summary>Sort by creation timestamp (default).</summary>
    CreatedAt = 0,

    /// <summary>Sort by last-mutation timestamp.</summary>
    UpdatedAt = 1,

    /// <summary>Sort by due date; <c>null</c> due dates collate last on ascending order.</summary>
    DueDate = 2,

    /// <summary>Sort by priority enum value (<c>Low</c> → <c>Critical</c>).</summary>
    Priority = 3,

    /// <summary>Sort alphabetically by title.</summary>
    Title = 4,

    /// <summary>Sort by the manual drag-and-drop <c>OrderKey</c>.</summary>
    Order = 5,
}

/// <summary>Sort direction for list endpoints.</summary>
public enum SortDirection
{
    /// <summary>Lowest value first.</summary>
    Ascending = 0,

    /// <summary>Highest value first.</summary>
    Descending = 1,
}
