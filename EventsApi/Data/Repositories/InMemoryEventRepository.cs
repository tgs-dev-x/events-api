using EventsApi.Models;
using System.Collections.Concurrent;

namespace EventsApi.Data.Repositories;

public class InMemoryEventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<int, Event> _store = new();
    private int _lastId = 0;
    public Event Add(Event model)
    {
        int newId = Interlocked.Increment(ref _lastId);

        model.Id = newId;
        _store.TryAdd(newId, model);

        return model;
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
            .Select(e => e.Clone());
    }

    public void Update(int id, Event model)
    {
        if (!_store.ContainsKey(id))
            return;

        model.Id = id;
        _store[id] = model;
    }
}
