namespace EventsApi.Dtos;

public record EventDto(Guid Id, string Title, string? Description, DateTime StartAt, DateTime EndAt);