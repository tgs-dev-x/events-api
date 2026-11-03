using EventsApi.Models;

namespace EventsApi.Data.Repositories;

public interface IBookingRepository
{
    IReadOnlyList<Booking> FindByPendingStatus();
    Booking? FindById(Guid id);
    Booking Add(Booking model);
    void Update(Guid id, Booking model);
}
