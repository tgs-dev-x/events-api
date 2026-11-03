using EventsApi.Models.Enums;

namespace EventsApi.Dtos;

public record BookingDto(
    Guid Id,
    Guid EventId,
    BookingStatus Status,
    string StatusDescription,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
