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
    private const int DefaultTotalSeats = 10;

    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(_bookingRepository,
            _eventRepository,
            new BookingMapper(),
            new BookingSynchronization());
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

    [Fact]
    public async Task CreateBookingAsync_AvailableSeats_DecreasesSeatsAndSavesEvent()
    {
        var eventData = GetStoredEvent(totalSeats: 3);
        ConfigureEventStorage(eventData);

        var result = await _service.CreateBookingAsync(eventData.Id);

        result.EventId.Should().Be(eventData.Id);
        result.Status.Should().Be(BookingStatus.Pending);

        _eventRepository.Received(1).Update(
            eventData.Id,
            Arg.Is<Event>(e => e.AvailableSeats == 2));

        _bookingRepository.Received(1).Add(
            Arg.Is<Booking>(b =>
                b.Id == result.Id &&
                b.EventId == eventData.Id));
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeats_ThrowsAndDoesNotSaveExtraBooking()
    {
        var eventData = GetStoredEvent(totalSeats: 1);
        ConfigureEventStorage(eventData);
        await _service.CreateBookingAsync(eventData.Id);

        Func<Task> act = () => _service.CreateBookingAsync(eventData.Id);

        await act.Should().ThrowAsync<NoAvailableSeatsException>();

        _bookingRepository.Received(1).Add(Arg.Any<Booking>());
        _eventRepository.Received(1).Update(
            eventData.Id, Arg.Any<Event>());

        _eventRepository.FindById(eventData.Id)!
            .AvailableSeats.Should().Be(0);
    }

    [Theory]
    [InlineData(5, 20, 5, 15)]
    [InlineData(10, 10, 10, 0)]
    public async Task CreateBookingAsync_ConcurrentRequests_RespectsCapacity(
        int totalSeats,
        int requestCount,
        int expectedSuccesses,
        int expectedFailures)
    {
        var eventData = GetStoredEvent(totalSeats: totalSeats);
        ConfigureEventStorage(eventData);

        var start = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                await start.Task;

                try
                {
                    var booking = await _service.CreateBookingAsync(eventData.Id);
                    return (Guid?)booking.Id;
                }
                catch (NoAvailableSeatsException)
                {
                    return (Guid?)null;
                }
            }))
            .ToArray();

        start.SetResult(true);
        var results = await Task.WhenAll(tasks);

        var successfulIds = results
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToArray();

        successfulIds.Should().HaveCount(expectedSuccesses)
            .And.OnlyHaveUniqueItems()
            .And.NotContain(Guid.Empty);

        results.Count(id => !id.HasValue)
            .Should().Be(expectedFailures);

        _bookingRepository.Received(expectedSuccesses)
            .Add(Arg.Any<Booking>());

        _eventRepository.Received(expectedSuccesses)
            .Update(eventData.Id, Arg.Any<Event>());

        _eventRepository.FindById(eventData.Id)!
            .AvailableSeats.Should().Be(totalSeats - expectedSuccesses);
    }

    [Fact]
    public async Task CreateBookingAsync_ReleaseSeats_AfterBookingRejected_RestoresAvailableSeats()
    {
        var eventData = GetStoredEvent(totalSeats: 1);
        ConfigureEventStorage(eventData);

        Booking? createdBooking = null;
        _bookingRepository.Add(Arg.Any<Booking>())
            .Returns(call =>
            {
                createdBooking = call.Arg<Booking>().Clone();
                return createdBooking.Clone();
            });

        await _service.CreateBookingAsync(eventData.Id);

        _eventRepository.FindById(eventData.Id)!
            .AvailableSeats.Should().Be(0);

        createdBooking!.Reject(DefaultProcessedAt);

        var currentEvent = _eventRepository.FindById(eventData.Id)!;
        currentEvent.ReleaseSeats();
        _eventRepository.Update(currentEvent.Id, currentEvent);

        _eventRepository.FindById(eventData.Id)!
            .AvailableSeats.Should().Be(1);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterRejectAndReleaseSeats_Succeeds()
    {
        var eventData = GetStoredEvent(totalSeats: 1);
        ConfigureEventStorage(eventData);

        Booking? createdBooking = null;
        _bookingRepository.Add(Arg.Any<Booking>())
            .Returns(call =>
            {
                createdBooking = call.Arg<Booking>().Clone();
                return createdBooking.Clone();
            });

        var first = await _service.CreateBookingAsync(eventData.Id);

        Func<Task> bookWhenFull =
            () => _service.CreateBookingAsync(eventData.Id);

        await bookWhenFull.Should()
            .ThrowAsync<NoAvailableSeatsException>();

        createdBooking!.Reject(DefaultProcessedAt);

        var currentEvent = _eventRepository.FindById(eventData.Id)!;
        currentEvent.ReleaseSeats();
        _eventRepository.Update(currentEvent.Id, currentEvent);

        var second = await _service.CreateBookingAsync(eventData.Id);

        second.Id.Should().NotBeEmpty().And.NotBe(first.Id);
        second.EventId.Should().Be(eventData.Id);
        second.Status.Should().Be(BookingStatus.Pending);
        second.ProcessedAt.Should().BeNull();

        _eventRepository.FindById(eventData.Id)!
            .AvailableSeats.Should().Be(0);
    }

    private static Event GetStoredEvent(
        Guid? id = null,
        string title = "Концерт",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = DefaultTotalSeats) => new
        (
            title,
            startAt ?? DefaultStart,
            endAt ?? DefaultEnd,
            totalSeats
        )
        {
            Id = id ?? DefaultEventId
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

    private void ConfigureEventStorage(Event initialEvent)
    {
        var stored = initialEvent.Clone();

        _eventRepository.FindById(initialEvent.Id)
            .Returns(_ => stored.Clone());

        _eventRepository
            .When(repository => repository.Update(
                initialEvent.Id, Arg.Any<Event>()))
            .Do(call => stored = call.ArgAt<Event>(1).Clone());

        _bookingRepository.Add(Arg.Any<Booking>())
            .Returns(call => call.Arg<Booking>().Clone());
    }
}
