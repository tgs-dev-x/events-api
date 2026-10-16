using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Data.Repositories;
using EventsApi.Filtering;
using EventsApi.Models;

namespace EventsApi.Tests.Data.Repositories;

public class InMemoryEventRepositoryTests
{
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEnd = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryEventRepository _repository = new();

    [Fact]
    public void Add_AssignsIncrementalId()
    {
        var first = SeedDefault("A");
        var second = SeedDefault("B");
        var third = SeedDefault("C");

        first.Id.Should().Be(1);
        second.Id.Should().Be(2);
        third.Id.Should().Be(3);
    }

    [Fact]
    public void FindById_ExistingById_ReturnsClone()
    {
        var stored = SeedDefault("Концерт");

        var result = _repository.FindById(stored.Id);

        result.Should().NotBeNull();
        result.Title.Should().Be("Концерт");
        result.Should().NotBeSameAs(stored);
    }

    [Fact]
    public void FindById_NonExistingById_ReturnsNull()
    {
        var result = _repository.FindById(999);

        result.Should().BeNull();
    }

    [Fact]
    public void Delete_ExistingById_ReturnsTrueAndRemoves()
    {
        var stored = SeedDefault("A");

        var result = _repository.Delete(stored.Id);

        result.Should().BeTrue();
        _repository.FindById(stored.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_NonExistingById_ReturnsFalse()
    {
        var result = _repository.Delete(999);

        result.Should().BeFalse();
    }

    [Fact]
    public void Update_ExistingById_ReplacesFields()
    {
        var newTitle = "New";
        var stored = SeedDefault("Old");
        var newStart = DefaultStart.AddDays(1);
        var newEnd = DefaultEnd.AddDays(1);

        _repository.Update(stored.Id, new Event
        {
            Title = newTitle,
            StartAt = newStart,
            EndAt = newEnd
        });

        var updated = _repository.FindById(stored.Id);
        updated!.Title.Should().Be(newTitle);
        updated.StartAt.Should().Be(newStart);
        updated.EndAt.Should().Be(newEnd);
    }

    [Fact]
    public void Update_DoesntChangeId()
    {
        var stored = SeedDefault("Old");

        _repository.Update(stored.Id, new Event
        {
            Id = 999,
            Title = "New",
            StartAt = DefaultStart,
            EndAt = DefaultEnd
        });

        var updated = _repository.FindById(stored.Id);
        updated!.Id.Should().Be(stored.Id);
    }

    [Fact]
    public void Update_NonExistingById_DoesNothing()
    {
        var notExistingId = 999;
        var act = () => _repository.Update(notExistingId, new Event
        {
            Title = "X",
            StartAt = DefaultStart,
            EndAt = DefaultEnd
        });

        act.Should().NotThrow();
        _repository.FindById(notExistingId).Should().BeNull();
    }

    [Fact]
    public void FindByFilter_EmptyStore_ReturnsEmpty()
    {
        var result = Find();

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void FindByFilter_NoCriteria_ReturnsAll()
    {
        SeedDefault("A");
        SeedDefault("B");
        SeedDefault("C");

        var result = Find();

        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Theory]
    [InlineData("конц")]
    [InlineData("КОНЦ")]
    [InlineData("Конц")]
    [InlineData("нцерт")]
    public void FindByFilter_TitleFilter_PartialAndCaseInsensitive(string filterTitle)
    {
        SeedDefault("Концерт");
        SeedDefault("Воркшоп");

        var result = Find(new EventSearchCriteria { Title = filterTitle });

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Концерт");
    }

    [Fact]
    public void FindByFilter_TitleFilter_NoMatches_ReturnsEmpty()
    {
        SeedDefault("Концерт");

        var result = Find(new EventSearchCriteria { Title = "qwerty" });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void FindByFilter_FromDate_ReturnsEventsStartingAtOrAfter()
    {
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Seed("До", cutoff.AddDays(-1), cutoff.AddDays(1));
        Seed("На границе", cutoff, cutoff.AddDays(2));
        Seed("После", cutoff.AddDays(1), cutoff.AddDays(3));

        var result = Find(new EventSearchCriteria { From = cutoff });

        result.Items.Should().HaveCount(2);
        result.Items.Select(e => e.Title)
            .Should()
            .Contain(["На границе", "После"]);
    }

    [Fact]
    public void FindByFilter_ToDate_ReturnsEventsEndingAtOrBefore()
    {
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Seed("До", cutoff.AddDays(-5), cutoff.AddDays(-1));
        Seed("На границе", cutoff.AddDays(-3), cutoff);
        Seed("После", cutoff.AddDays(1), cutoff.AddDays(3));

        var result = Find(new EventSearchCriteria { To = cutoff });

        result.Items.Should().HaveCount(2);
        result.Items.Select(e => e.Title)
            .Should()
            .Contain(["До", "На границе"]);
    }

    [Fact]
    public void FindByFilter_CombinedFilters_AllApplied()
    {
        var inRange = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);

        Seed("Концерт 1", inRange, inRange.AddHours(2));
        Seed("Концерт 2", new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        Seed("Выступление", inRange, inRange.AddHours(2));

        var result = Find(new EventSearchCriteria
        {
            Title = "конц",
            From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Концерт 1");
    }

    [Fact]
    public void FindByFilter_NoSorts_DefaultsToStartAtAsc()
    {
        var later = DefaultStart.AddDays(5);
        Seed("Позже", later, later.AddHours(2));
        Seed("Раньше", DefaultStart, DefaultEnd);

        var result = Find();

        result.Items[0].Title.Should().Be("Раньше");
        result.Items[1].Title.Should().Be("Позже");
    }

    [Fact]
    public void FindByFilter_SortByTitleAsc()
    {
        SeedDefault("Выступление");
        SeedDefault("Концерт");
        SeedDefault("Конференция");

        var result = Find(sorts:
            [
                new SortOrder<EventSortField> { Field = EventSortField.Title }
            ]);

        result.Items.Select(e => e.Title)
            .Should()
            .BeInAscendingOrder();
    }

    [Fact]
    public void FindByFilter_SortByTitleDesc()
    {
        SeedDefault("Выступление");
        SeedDefault("Концерт");
        SeedDefault("Конференция");

        var result = Find(sorts:
        [
            new SortOrder<EventSortField> { Field = EventSortField.Title, Descending = true }
        ]);

        result.Items.Select(e => e.Title)
            .Should()
            .BeInDescendingOrder();
    }

    [Fact]
    public void FindByFilter_Pagination_FirstPage()
    {
        for (var i = 1; i <= 25; i++)
            Seed($"Event {i}", DefaultStart.AddDays(i), DefaultEnd.AddDays(i));

        var result = Find(page: new PageParameters { Page = 1, PageSize = 10 });

        result.TotalCount.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public void FindByFilter_Pagination_BeyondRange_ReturnsEmpty()
    {
        SeedDefault("Концерт");

        var result = Find(page: new PageParameters { Page = 100, PageSize = 10 });

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public void FindByFilter_Pagination_InvalidPage_NormalizedToDefault()
    {
        SeedDefault("A");

        var result = Find(page: new PageParameters { Page = 0, PageSize = 10 });

        result.Page.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public void FindByFilter_Pagination_InvalidPageSize_NormalizedToMax()
    {
        for (var i = 1; i <= 5; i++)
            Seed($"Event {i}", DefaultStart.AddDays(i), DefaultEnd.AddDays(i));

        var result = Find(page: new PageParameters { Page = 1, PageSize = 99999 });

        result.PageSize.Should().Be(PageParameters.MaxPageSize);
        result.Items.Should().HaveCount(5);
    }

    private Event Seed(string title, DateTime start, DateTime end) =>
        _repository.Add(new Event { Title = title, StartAt = start, EndAt = end });

    private Event SeedDefault(string title) => Seed(title, DefaultStart, DefaultEnd);

    private PaginatedResult<Event> Find(EventSearchCriteria? criteria = null,
        IReadOnlyList<SortOrder<EventSortField>>? sorts = null,
        PageParameters? page = null) =>
            _repository.FindByFilter(
                criteria ?? new EventSearchCriteria(),
                sorts ?? [],
                page ?? new PageParameters());
}