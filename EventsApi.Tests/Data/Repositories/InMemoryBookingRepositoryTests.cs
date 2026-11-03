using EventsApi.Data.Repositories;
using EventsApi.Models;
using EventsApi.Models.Enums;

namespace EventsApi.Tests.Data.Repositories;

public class InMemoryBookingRepositoryTests
{
    private static readonly Guid DefaultEventId = new("11111111-2222-3333-4444-555555555555");
    private static readonly DateTime DefaultCreatedAt = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryBookingRepository _repository = new();

    [Fact]
    public void Add_KeepsProvidedId()
    {
        var model = Booking.CreatePending(DefaultEventId, DefaultCreatedAt);
        var expectedId = model.Id;

        var stored = _repository.Add(model);

        stored.Id.Should().Be(expectedId);
    }

    [Fact]
    public void FindById_ExistingById_ReturnsClone()
    {
        var stored = SeedDefault();

        var result = _repository.FindById(stored.Id);

        result.Should().NotBeNull();
        result.Should().NotBeSameAs(stored);
    }

    [Fact]
    public void FindById_NonExistingById_ReturnsNull()
    {
        var result = _repository.FindById(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public void FindByPendingStatus_EmptyStore_ReturnsEmpty()
    {
        var result = _repository.FindByPendingStatus();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindByPendingStatus_ReturnsOnlyPending()
    {
        SeedWithStatus(BookingStatus.Pending);
        SeedWithStatus(BookingStatus.Pending);
        SeedWithStatus(BookingStatus.Confirmed);
        SeedWithStatus(BookingStatus.Rejected);

        var result = _repository.FindByPendingStatus();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.Status == BookingStatus.Pending);
    }

    [Fact]
    public void FindByPendingStatus_ReturnsClones()
    {
        var stored = SeedDefault();

        var result = _repository.FindByPendingStatus();

        result.Should().ContainSingle();
        result[0].Should().NotBeSameAs(stored);
    }

    [Fact]
    public void Update_ExistingById_ReplacesFields()
    {
        var stored = SeedDefault();
        var newCreatedAt = DefaultCreatedAt.AddDays(1);
        var newProcessedAt = DefaultCreatedAt.AddDays(2);

        _repository.Update(stored.Id, new Booking
        {
            EventId = DefaultEventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = newCreatedAt,
            ProcessedAt = newProcessedAt
        });

        var updated = _repository.FindById(stored.Id);
        updated!.Status.Should().Be(BookingStatus.Confirmed);
        updated.CreatedAt.Should().Be(newCreatedAt);
        updated.ProcessedAt.Should().Be(newProcessedAt);
    }

    [Fact]
    public void Update_DoesntChangeId()
    {
        var stored = SeedDefault();

        _repository.Update(stored.Id, new Booking
        {
            Id = Guid.NewGuid(),
            EventId = DefaultEventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DefaultCreatedAt
        });

        var updated = _repository.FindById(stored.Id);
        updated!.Id.Should().Be(stored.Id);
    }

    [Fact]
    public void Update_NonExistingById_DoesNothing()
    {
        var notExistingId = Guid.NewGuid();
        var act = () => _repository.Update(notExistingId, new Booking
        {
            EventId = DefaultEventId,
            Status = BookingStatus.Confirmed,
            CreatedAt = DefaultCreatedAt
        });

        act.Should().NotThrow();
        _repository.FindById(notExistingId).Should().BeNull();
    }

    private Booking SeedDefault(DateTime? createdAt = null)
        => SeedWithStatus(BookingStatus.Pending, createdAt);

    private Booking SeedWithStatus(BookingStatus status, DateTime? createdAt = null)
    {
        var booking = Booking.CreatePending(DefaultEventId, createdAt ?? DefaultCreatedAt);

        switch (status)
        {
            case BookingStatus.Confirmed:
                booking.Confirm(DefaultCreatedAt.AddHours(1));
                break;
            case BookingStatus.Rejected:
                booking.Reject(DefaultCreatedAt.AddHours(1));
                break;
        }

        return _repository.Add(booking);
    }
}
