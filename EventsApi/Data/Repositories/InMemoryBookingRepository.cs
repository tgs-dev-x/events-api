using EventsApi.Models;
using EventsApi.Models.Enums;
using System.Collections.Concurrent;

namespace EventsApi.Data.Repositories;

public class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _store = new();

    public Booking Add(Booking model)
    {
        var stored = model.Clone();

        if (!_store.TryAdd(stored.Id, stored))
            throw new InvalidOperationException($"Не удалось добавить бронь {stored.Id}");

        return stored.Clone();
    }

    public Booking? FindById(Guid id)
    {
        return _store.TryGetValue(id, out var foundBooking) ? foundBooking.Clone() : null;
    }

    public IReadOnlyList<Booking> FindByPendingStatus()
    {
        return _store.Values
            .Where(b => BookingStatus.Pending == b.Status)
            .Select(e => e.Clone())
            .ToList();
    }

    public void Update(Guid id, Booking model)
    {
        var stored = model.Clone();
        stored.Id = id;

        if (_store.TryGetValue(id, out var current))
            _store.TryUpdate(id, stored, current);
    }
}
