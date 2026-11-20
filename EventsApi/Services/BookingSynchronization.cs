namespace EventsApi.Services;

public class BookingSynchronization
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}
