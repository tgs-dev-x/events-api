using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Filtering;
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

    public PaginatedResult<Event> FindByFilter(
        EventSearchCriteria criteria,
        IReadOnlyList<SortOrder<EventSortField>> sorts,
        PageParameters pageParameters)
    {
        IEnumerable<Event> query = _store.Values;

        query = ApplySearchCriteria(query, criteria);
        query = ApplySort(query, sorts);

        var totalCount = query.Count();

        query = ApplyPagination(query, pageParameters);

        var items = query
            .Select(e => e.Clone())
            .ToList();

        return new PaginatedResult<Event>
        {
            Items = items,
            Page = pageParameters.GetNormalizedPage(),
            PageSize = pageParameters.GetNormalizedPageSize(),
            TotalCount = totalCount
        };
    }

    private IEnumerable<Event> ApplySearchCriteria(IEnumerable<Event> source, EventSearchCriteria criteria)
    {
        if (criteria == null)
            return source;

        IEnumerable<Event> result = source;
        if (!string.IsNullOrEmpty(criteria.Title))
            result = result.Where(e =>
                e.Title.Contains(criteria.Title, StringComparison.OrdinalIgnoreCase));

        if (criteria.From.HasValue)
            result = result.Where(e => e.StartAt >= criteria.From.Value);

        if (criteria.To.HasValue)
            result = result.Where(e => e.EndAt <= criteria.To.Value);

        return result;
    }

    private IEnumerable<Event> ApplySort(IEnumerable<Event> source, IReadOnlyList<SortOrder<EventSortField>> sorts)
    {
        if (sorts == null || sorts.Count == 0)
            return source.OrderBy(e => e.StartAt)
                .ThenBy(e => e.Id);

        IOrderedEnumerable<Event>? ordered = null;

        foreach (var sort in sorts)
        {
            var key = GetSortKeySelector(sort.Field);

            if (ordered is null)
                ordered = sort.Descending
                    ? source.OrderByDescending(key)
                    : source.OrderBy(key);

            else
                ordered = sort.Descending
                    ? ordered.ThenByDescending(key)
                    : ordered.ThenBy(key);

        }

        return ordered!;
    }

    private Func<Event, IComparable> GetSortKeySelector(EventSortField field) => field switch
    {
        EventSortField.StartAt => e => e.StartAt,
        EventSortField.EndAt => e => e.EndAt,
        EventSortField.Title => e => e.Title,
        _ => e => e.StartAt
    };

    private IEnumerable<Event> ApplyPagination(IEnumerable<Event> source, PageParameters pageParameters)
    {
        return source
            .Skip(pageParameters.GetSkip())
            .Take(pageParameters.GetNormalizedPageSize());
    }

    public void Update(int id, Event model)
    {
        var stored = model.Clone();
        stored.Id = id;

        if (_store.TryGetValue(id, out var current))
            _store.TryUpdate(id, stored, current);
    }
}
