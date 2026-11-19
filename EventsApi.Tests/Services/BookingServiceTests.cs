using EventsApi.Data.Repositories;
using EventsApi.Exceptions;
using EventsApi.Mappers;
using EventsApi.Models;
using EventsApi.Models.Enums;
using EventsApi.Services;

namespace EventsApi.Tests.Services;

public class BookingServiceTests
{
    private static readonly Guid DefaultId = new("66666666-7777-8888-9999-aaaaaaaaaaaa");
    private static readonly Guid DefaultEventId = new("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime DefaultCreatedAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultProcessedAt = new(2026, 6, 1, 10, 0, 2, DateTimeKind.Utc);
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEnd = new(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc);

    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(_bookingRepository,
            _eventRepository,
            new BookingMapper());
    }

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsPendingDtoAndCallsAdd()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.FindById(eventId).Returns(GetStoredEvent(id: eventId));
        _bookingRepository.Add(Arg.Any<Booking>())
            .Returns(GetStoredBooking(eventId: eventId));

        var result = await _service.CreateBookingAsync(eventId);

        result.Status.Should().Be(BookingStatus.Pending);
        result.EventId.Should().Be(eventId);
        result.ProcessedAt.Should().BeNull();
        _eventRepository.Received(1).FindById(eventId);
        _bookingRepository.Received(1).Add(
            Arg.Is<Booking>(b => b.EventId == eventId && b.Status == BookingStatus.Pending));
    }

    [Fact]
    public async Task CreateBookingAsync_NonExistingEvent_ThrowsNotFoundExAndDoesntCallAdd()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.FindById(eventId).Returns((Event?)null);

        Func<Task> act = () => _service.CreateBookingAsync(eventId);

        await act.Should().ThrowAsync<NotFoundException>();
        _bookingRepository.DidNotReceive().Add(Arg.Any<Booking>());
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleForSameEvent_ReturnsUniqueIds()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.FindById(eventId).Returns(GetStoredEvent(id: eventId));
        _bookingRepository.Add(Arg.Any<Booking>())
        .Returns(ci => ci.Arg<Booking>());

        var first = await _service.CreateBookingAsync(eventId);
        var second = await _service.CreateBookingAsync(eventId);
        var third = await _service.CreateBookingAsync(eventId);

        first.Id.Should().NotBe(Guid.Empty);
        second.Id.Should().NotBe(Guid.Empty);
        third.Id.Should().NotBe(Guid.Empty);
        first.Id.Should().NotBe(second.Id);
        second.Id.Should().NotBe(third.Id);
        first.Id.Should().NotBe(third.Id);
        first.EventId.Should().Be(eventId);
        second.EventId.Should().Be(eventId);
        third.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Existing_ReturnsMappedDto()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepository.FindById(bookingId)
            .Returns(GetStoredBooking(id: bookingId, eventId: DefaultEventId));

        var result = await _service.GetBookingByIdAsync(bookingId);

        result.Id.Should().Be(bookingId);
        result.EventId.Should().Be(DefaultEventId);
        result.Status.Should().Be(BookingStatus.Pending);
        result.ProcessedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public async Task GetBookingByIdAsync_AfterStatusChange_ReturnsUpdatedStatus(BookingStatus newStatus)
    {
        var bookingId = Guid.NewGuid();
        _bookingRepository.FindById(bookingId)
            .Returns(GetStoredBooking(id: bookingId, status: newStatus));

        var result = await _service.GetBookingByIdAsync(bookingId);

        result.Status.Should().Be(newStatus);
        result.ProcessedAt.Should().Be(DefaultProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExisting_ThrowsNotFoundEx()
    {
        var bookingId = Guid.NewGuid();
        _bookingRepository.FindById(bookingId).Returns((Booking?)null);

        Func<Task> act = () => _service.GetBookingByIdAsync(bookingId);

        await act.Should().ThrowAsync<NotFoundException>();
    }


    private static Event GetStoredEvent(
        Guid? id = null,
        string title = "Концерт",
        DateTime? startAt = null,
        DateTime? endAt = null) => new()
        {
            Id = id ?? DefaultEventId,
            Title = title,
            StartAt = startAt ?? DefaultStart,
            EndAt = endAt ?? DefaultEnd
        };

    private static Booking GetStoredBooking(
        Guid? id = null,
        Guid? eventId = null,
        BookingStatus status = BookingStatus.Pending)
    {
        var booking = Booking.CreatePending(eventId ?? DefaultEventId, DefaultCreatedAt);
        booking.Id = id ?? DefaultId;

        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm(DefaultProcessedAt);
                break;
            case BookingStatus.Rejected:
                booking.Reject(DefaultProcessedAt);
                break;
        }

        return booking;
    }
}
