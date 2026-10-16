namespace EventsApi.Common.Pagination;

public sealed record PaginatedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page {  get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;
}
