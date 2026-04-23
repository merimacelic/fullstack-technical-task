namespace TaskManagement.Application.Common.Pagination;

public sealed record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 20,
        > MaxPageSize => MaxPageSize,
        _ => PageSize,
    };

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
