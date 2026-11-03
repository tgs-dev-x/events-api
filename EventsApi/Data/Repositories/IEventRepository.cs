using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Filtering;
using EventsApi.Models;

namespace EventsApi.Data.Repositories;

public interface IEventRepository
{
    PaginatedResult<Event> FindByFilter(
        EventSearchCriteria criteria,
        IReadOnlyList<SortOrder<EventSortField>> sorts,
        PageParameters pageParameters);
    Event? FindById(Guid id);
    Event Add(Event model);
    void Update(Guid id, Event model);
    bool Delete(Guid id);
}
