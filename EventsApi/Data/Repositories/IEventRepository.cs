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
    Event? FindById(int id);
    Event Add(Event model);
    void Update(int id, Event model);
    bool Delete(int id);
}
