using EventsApi.Models;

namespace EventsApi.Data.Repositories;

public interface IEventRepository
{
    IEnumerable<Event> GetAll();
    Event? FindById(int id);
    Event Add(Event model);
    bool Update(int id, Event model);
    bool Delete(int id);
    bool ExistsById(int id);
}
