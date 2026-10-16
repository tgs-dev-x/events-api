namespace EventsApi.Filtering;

public record EventSearchCriteria(string? Title, DateTime? From, DateTime? To);
