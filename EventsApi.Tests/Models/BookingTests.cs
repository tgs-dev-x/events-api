using EventsApi.Models;
using EventsApi.Models.Enums;

namespace EventsApi.Tests.Models;

public class BookingTests
{
    [Fact]
    public void CreatePending_SetsDefaults()
    {
        var eventId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var booking = Booking.CreatePending(eventId, now);

        booking.Id.Should().NotBe(Guid.Empty);
        booking.EventId.Should().Be(eventId);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().Be(now);
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Confirm_SetsStatusAndProcessedAt()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), DateTime.UtcNow);
        var now = DateTime.UtcNow.AddSeconds(2);

        booking.Confirm(now);

        booking.Status.Should().Be(BookingStatus.Confirmed);
        booking.ProcessedAt.Should().Be(now);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public void Confirm_NotPending_ThrowsInvalidOperationEx(BookingStatus currentStatus)
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), DateTime.UtcNow);

        switch (currentStatus)
        {
            case BookingStatus.Confirmed:
                booking.Confirm(DateTime.UtcNow);
                break;
            case BookingStatus.Rejected:
                booking.Reject(DateTime.UtcNow);
                break;
        }

        var act = () => booking.Confirm(DateTime.UtcNow.AddSeconds(1));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_SetsStatusAndProcessedAt()
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), DateTime.UtcNow);
        var now = DateTime.UtcNow.AddSeconds(2);

        booking.Reject(now);

        booking.Status.Should().Be(BookingStatus.Rejected);
        booking.ProcessedAt.Should().Be(now);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public void Reject_NotPending_ThrowsInvalidOperationEx(BookingStatus currentStatus)
    {
        var booking = Booking.CreatePending(Guid.NewGuid(), DateTime.UtcNow);

        switch (currentStatus)
        {
            case BookingStatus.Confirmed:
                booking.Confirm(DateTime.UtcNow);
                break;
            case BookingStatus.Rejected:
                booking.Reject(DateTime.UtcNow);
                break;
        }

        var act = () => booking.Reject(DateTime.UtcNow.AddSeconds(1));

        act.Should().Throw<InvalidOperationException>();
    }
}
