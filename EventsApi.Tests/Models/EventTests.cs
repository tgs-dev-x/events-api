using EventsApi.Exceptions;
using EventsApi.Models;

namespace EventsApi.Tests.Models;

public class EventTests
{
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime DefaultEnd = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ValidTotalSeats_AllSeatsAvailable()
    {
        var model = CreateEvent(totalSeats: 5);

        model.TotalSeats.Should().Be(5);
        model.AvailableSeats.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidTotalSeats_ThrowsDataValidationEx(
        int totalSeats)
    {
        var act = () => CreateEvent(totalSeats);

        act.Should().Throw<DataValidationException>();
    }

    [Fact]
    public void TryReserveSeats_DefaultCount_ReservesOneSeat()
    {
        var model = CreateEvent(totalSeats: 3);

        var result = model.TryReserveSeats();

        result.Should().BeTrue();
        model.AvailableSeats.Should().Be(2);
        model.TotalSeats.Should().Be(3);
    }

    [Fact]
    public void TryReserveSeats_ExactAvailableCount_ReservesAllSeats()
    {
        var model = CreateEvent(totalSeats: 3);

        var result = model.TryReserveSeats(count: 3);

        result.Should().BeTrue();
        model.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void TryReserveSeats_InsufficientSeats_DoesNotChangeAvailableSeats()
    {
        var model = CreateEvent(totalSeats: 3);
        model.TryReserveSeats();

        var result = model.TryReserveSeats(count: 3);

        result.Should().BeFalse();
        model.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public void TryReserveSeats_NoSeats_ReturnsFalse()
    {
        var model = CreateEvent(totalSeats: 1);
        model.TryReserveSeats();

        var result = model.TryReserveSeats();

        result.Should().BeFalse();
        model.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public void ReleaseSeats_DefaultCount_RestoresOneSeat()
    {
        var model = CreateEvent(totalSeats: 3);
        model.TryReserveSeats(count: 2);

        model.ReleaseSeats();

        model.AvailableSeats.Should().Be(2);
        model.TotalSeats.Should().Be(3);
    }

    [Fact]
    public void ReleaseSeats_MultipleSeats_RestoresSpecifiedCount()
    {
        var model = CreateEvent(totalSeats: 5);
        model.TryReserveSeats(count: 4);

        model.ReleaseSeats(count: 3);

        model.AvailableSeats.Should().Be(4);
    }

    [Fact]
    public void TryReserveSeats_AfterRelease_CanReserveAgain()
    {
        var model = CreateEvent(totalSeats: 1);
        model.TryReserveSeats();

        model.ReleaseSeats();
        var result = model.TryReserveSeats();

        result.Should().BeTrue();
        model.AvailableSeats.Should().Be(0);
    }

    [Theory]
    [InlineData(8, 6)]
    [InlineData(3, 1)]
    [InlineData(2, 0)]
    public void ChangeTotalSeats_ValidCapacity_PreservesReservedSeats(
        int totalSeats,
        int expectedAvailableSeats)
    {
        var model = CreateEvent(totalSeats: 5);
        model.TryReserveSeats(count: 2);

        model.ChangeTotalSeats(totalSeats);

        model.TotalSeats.Should().Be(totalSeats);
        model.AvailableSeats.Should().Be(expectedAvailableSeats);
    }

    [Fact]
    public void ChangeTotalSeats_BelowReservedCount_ThrowsAndDoesNotChangeState()
    {
        var model = CreateEvent(totalSeats: 5);
        model.TryReserveSeats(count: 3);

        var act = () => model.ChangeTotalSeats(2);

        act.Should().Throw<DataValidationException>();
        model.TotalSeats.Should().Be(5);
        model.AvailableSeats.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeTotalSeats_InvalidCapacity_ThrowsAndDoesNotChangeState(
        int totalSeats)
    {
        var model = CreateEvent(totalSeats: 5);
        model.TryReserveSeats();

        var act = () => model.ChangeTotalSeats(totalSeats);

        act.Should().Throw<DataValidationException>();
        model.TotalSeats.Should().Be(5);
        model.AvailableSeats.Should().Be(4);
    }

    private static Event CreateEvent(int totalSeats) =>
        new("Концерт", DefaultStart, DefaultEnd, totalSeats);
}
