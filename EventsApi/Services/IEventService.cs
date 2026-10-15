using EventsApi.Dtos;

namespace EventsApi.Services;

public interface IEventService
{
    List<EventDto> GetAll();
    EventDto GetById(int id);
    EventDto Create(EventSaveDto? saveDto);
    void Update(int id, EventSaveDto? saveDto);
    void Delete(int id);

}
