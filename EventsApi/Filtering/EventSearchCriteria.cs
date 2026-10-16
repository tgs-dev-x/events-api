namespace EventsApi.Filtering;

public record EventSearchCriteria
{
    public string? Title { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}
