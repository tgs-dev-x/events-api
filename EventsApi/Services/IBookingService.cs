using EventsApi.Dtos;

namespace EventsApi.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId);
}
