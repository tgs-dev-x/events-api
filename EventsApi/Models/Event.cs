namespace EventsApi.Models;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public Event Clone() => new()
    {
        Id = Id,
        Title = Title,
        Description = Description,
        StartAt = StartAt,
        EndAt = EndAt
    };
}
