using EventsApi.Models.Enums;

namespace EventsApi.Models;

public class Booking
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public Booking Clone() => (Booking)MemberwiseClone();

    public void Confirm(DateTime now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Нельзя подтвердить бронь в статусе {Status}");

        Status = BookingStatus.Confirmed;
        ProcessedAt = now;
    }

    public void Reject(DateTime now)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Нельзя отклонить бронь в статусе {Status}");

        Status = BookingStatus.Rejected;
        ProcessedAt = now;
    }

    public static Booking CreatePending(Guid eventId, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        EventId = eventId,
        Status = BookingStatus.Pending,
        CreatedAt = now,
        ProcessedAt = null
    };
}
