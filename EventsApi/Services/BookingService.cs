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

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository, BookingMapper bookingMapper)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _bookingMapper = bookingMapper;
    }

    public Task<BookingDto> CreateBookingAsync(Guid eventId)
    {
        _ = _eventRepository.FindById(eventId)
            ?? throw new NotFoundException(nameof(Event), eventId);

        var pending = Booking.CreatePending(eventId, DateTime.UtcNow);
        var saved = _bookingRepository.Add(pending);

        var dto = _bookingMapper.ToDto(saved);
        return Task.FromResult(dto);
    }

    public Task<BookingDto> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookingRepository.FindById(bookingId)
            ?? throw new NotFoundException(nameof(Booking), bookingId);

        var dto = _bookingMapper.ToDto(booking);
        return Task.FromResult(dto);
    }
}
