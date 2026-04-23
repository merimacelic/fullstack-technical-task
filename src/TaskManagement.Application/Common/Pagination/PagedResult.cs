namespace TaskManagement.Application.Common.Pagination;

/// <summary>Paged envelope returned by list endpoints.</summary>
/// <typeparam name="T">Item type for the page.</typeparam>
/// <param name="Items">Page slice, in the sort order requested by the caller.</param>
/// <param name="Page">1-based page number that this slice belongs to.</param>
/// <param name="PageSize">Size requested (and used) for the page.</param>
/// <param name="TotalCount">Total number of items available across all pages.</param>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>Total pages available for the given <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>True when a caller can request <c>Page + 1</c>.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>True when a caller can request <c>Page - 1</c>.</summary>
    public bool HasPreviousPage => Page > 1;
}
