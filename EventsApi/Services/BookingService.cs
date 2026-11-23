using EventsApi.Data.Repositories;
using EventsApi.Dtos;
using EventsApi.Exceptions;
using EventsApi.Mappers;
using EventsApi.Models;

namespace EventsApi.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly BookingMapper _bookingMapper;

    private readonly BookingSynchronization _synchronization;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, BookingMapper bookingMapper, BookingSynchronization synchronization)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _bookingMapper = bookingMapper;
        _synchronization = synchronization;
    }

    public async Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        await _synchronization.Gate.WaitAsync();
        try
        {
            var currentEvent = _eventRepository.FindById(eventId)
                ?? throw new NotFoundException(nameof(Event), eventId);

            if (!currentEvent.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var pending = Booking.CreatePending(eventId, DateTime.UtcNow);
            var saved = _bookingRepository.Add(pending);
            _eventRepository.Update(eventId, currentEvent);

            return _bookingMapper.ToDto(saved);
        }
        finally
        {
            _synchronization.Gate.Release();
        }
    }

    public Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookingRepository.FindById(bookingId)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        var dto = _bookingMapper.ToDto(booking);
        return Task.FromResult(dto);
    }
}
