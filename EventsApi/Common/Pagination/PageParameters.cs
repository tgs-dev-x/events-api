namespace EventsApi.Common.Pagination;

public sealed record PageParameters
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    
    public int Page { get; init; } = DefaultPage;
    public int PageSize { get; init; } = DefaultPageSize;
}
