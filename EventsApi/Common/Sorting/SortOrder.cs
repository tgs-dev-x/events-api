namespace EventsApi.Common.Sorting;

public sealed record SortOrder<TField> where TField: struct, Enum
{
    public required TField Field { get; init; }
    public bool Descending { get; init; }
}
