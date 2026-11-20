using EventsApi.Exceptions;

namespace EventsApi.Models;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats { get; private set; }

    public Event Clone() => (Event)MemberwiseClone();

    public Event(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        if (totalSeats <= 0)
            throw new DataValidationException($"{nameof(TotalSeats)} должен быть больше 0");

        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (count < 0)
            return false;

        if (AvailableSeats <= 0)
            return false;

        if (AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (count < 0)
            throw new ArgumentException("Количество должно быть больше 0");

        AvailableSeats += count;
    }

    public void ChangeTotalSeats(int totalSeats)
    {
        if (totalSeats <= 0)
            throw new DataValidationException("Количество мест должно быть больше 0");

        var reservedSeats = TotalSeats - AvailableSeats;

        if (totalSeats < reservedSeats)
            throw new DataValidationException("Количество мест не может быть меньше уже забронированного");

        TotalSeats = totalSeats;
        AvailableSeats = totalSeats - reservedSeats;
    }
}
