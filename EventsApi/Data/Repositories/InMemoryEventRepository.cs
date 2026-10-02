using EventsApi.Models;
using System.Collections.Concurrent;

namespace EventsApi.Data.Repositories;

public class InMemoryEventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<int, Event> _store = new();
    private int _lastId = 0;
    public Event Add(Event model)
    {
        var stored = model.Clone();
        int newId = Interlocked.Increment(ref _lastId);

        stored.Id = newId;
        _store.TryAdd(newId, stored);

        return stored.Clone();
    }

    public bool Delete(int id)
    {
        return _store.TryRemove(id, out _);
    }

    public Event? FindById(int id)
    {
        return _store.TryGetValue(id, out var foundEvent) ? foundEvent.Clone() : null;
    }

    public IEnumerable<Event> GetAll()
    {
        return _store.Values
            .OrderBy(e => e.StartAt)
            .Select(e => e.Clone())
            .ToList();
    }

    public void Update(int id, Event model)
    {
        var stored = model.Clone();
        stored.Id = id;

        if (_store.TryGetValue(id, out var current))
            _store.TryUpdate(id, stored, current);
    }
}
