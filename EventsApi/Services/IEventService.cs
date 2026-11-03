using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Dtos;
using EventsApi.Filtering;

namespace EventsApi.Services;

public interface IEventService
{
    PaginatedResult<EventDto> GetByFilter(EventSearchCriteria criteria, 
        IReadOnlyList<SortOrder<EventSortField>> sorts, 
        PageParameters pageParameters);
    EventDto GetById(Guid id);
    EventDto Create(EventSaveDto? saveDto);
    void Update(Guid id, EventSaveDto? saveDto);
    void Delete(Guid id);

}
