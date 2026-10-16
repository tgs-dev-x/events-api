using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Data.Repositories;
using EventsApi.Dtos;
using EventsApi.Exceptions;
using EventsApi.Filtering;
using EventsApi.Mappers;
using EventsApi.Models;
using EventsApi.Services;
using EventsApi.Validation;

namespace EventsApi.Tests.Services;

public class EventServiceTests
{
    private static readonly DateTime DefaultStart = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DefaultEnd = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService(new EventMapper(), 
            _repository,
            new DataAnnotationsModelValidator());
    }

    [Fact]
    public void Create_ValidDto_CallsAddAndReturnsMappedResult()
    {
        _repository.Add(Arg.Any<Event>())
            .Returns(GetStoredEvent());

        var result = _service.Create(GetValidDto());

        result.Id.Should().Be(1);
        result.Title.Should().Be("Концерт");
        _repository.Received(1).Add(Arg.Any<Event>());
    }

    [Fact]
    public void Create_NullDto_ThrowsDataValidationExAndDoesntCallRepo()
    {
        var act = () => _service.Create(null);

        act.Should().Throw<DataValidationException>();
        _repository.DidNotReceive().Add(Arg.Any<Event>());
    }

    [Fact]
    public void Create_EndBeforeStart_ThrowsDataValidationExAndDoesntCallRepo()
    {
        var dto = new EventSaveDto
        {
            Title = "Концерт",
            StartAt = DefaultEnd,
            EndAt = DefaultStart
        };

        var act = () => _service.Create(dto);
        act.Should().Throw<DataValidationException>();
        _repository.DidNotReceive().Add(Arg.Any<Event>());
    }

    [Fact]
    public void Create_EndEqualsStart_ThrowsDataValidationEx()
    {
        var dto = new EventSaveDto
        {
            Title = "Концерт",
            StartAt = DefaultStart,
            EndAt = DefaultStart
        };

        var act = () => _service.Create(dto);

        act.Should().Throw<DataValidationException>();
    }

    [Fact]
    public void Create_MissingTitle_ThrowsDataValidationEx()
    {
        var dto = new EventSaveDto
        {
            Title = null,
            StartAt = DefaultStart,
            EndAt = DefaultEnd
        };

        var act = () => _service.Create(dto);

        act.Should().Throw<DataValidationException>();
    }

    [Fact]
    public void GetById_ExistingById_ReturnsMappedDto()
    {
        var existingId = 1;
        _repository.FindById(1)
            .Returns(GetStoredEvent(id: existingId, title: "X"));

        var result = _service.GetById(existingId);

        result.Id.Should().Be(existingId);
        result.Title.Should().Be("X");
    }

    [Fact]
    public void GetById_NonExistingById_ThrowsNotFoundEx()
    {
        var notExistingId = 999;

        _repository.FindById(notExistingId)
            .Returns((Event?)null);

        var act = () => _service.GetById(notExistingId);
        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void Update_ExistingById_CallsRepositoryUpdate()
    {
        var existingId = 1;
        var newTitle = "New";
        _repository.FindById(1).Returns(GetStoredEvent(id: existingId, title: "Old"));

        _service.Update(existingId, GetValidDto(newTitle));

        _repository.Received(1).Update(existingId, Arg.Is<Event>(e => e.Title == newTitle));
    }

    [Fact]
    public void Update_NonExistingById_ThrowsNotFoundExAndDoesntCallUpdate()
    {
        var notExistingId = 999;
        _repository.FindById(notExistingId)
            .Returns((Event?)null);

        var act = () => _service.Update(notExistingId, GetValidDto());

        act.Should().Throw<NotFoundException>();
        _repository.DidNotReceive().Update(Arg.Any<int>(), Arg.Any<Event>());
    }

    [Fact]
    public void Update_EndBeforeStart_ThrowsDataValidationExAndDoesntCallUpdate()
    {
        var dto = new EventSaveDto
        {
            Title = "Концерт",
            StartAt = DefaultEnd,
            EndAt = DefaultStart
        };

        var act = () => _service.Update(1, dto);

        act.Should().Throw<DataValidationException>();
        _repository.DidNotReceive().Update(Arg.Any<int>(), Arg.Any<Event>());
    }

    [Fact]
    public void Update_EndEqualsStart_ThrowsDataValidationEx()
    {
        var dto = new EventSaveDto
        {
            Title = "Концерт",
            StartAt = DefaultStart,
            EndAt = DefaultStart
        };

        var act = () => _service.Update(1, dto);

        act.Should().Throw<DataValidationException>();
    }

    [Fact]
    public void Delete_NonExistingById_ThrowsNotFoundEx()
    {
        var notExistingId = 999;
        _repository.Delete(notExistingId).Returns(false);

        var act = () => _service.Delete(notExistingId);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void Delete_ExistingById_CallsRepositoryDelete()
    {
        var existingId = 1;
        _repository.Delete(existingId).Returns(true);

        var act = () => _service.Delete(existingId);

        act.Should().NotThrow();
        _repository.Received(1).Delete(existingId);
    }

    [Fact]
    public void GetByFilter_MapsRepositoryResultToDto()
    {
        var storedId = 1;
        var storedTitle = "X";

        _repository
            .FindByFilter(
                Arg.Any<EventSearchCriteria>(),
                Arg.Any<IReadOnlyList<SortOrder<EventSortField>>>(),
                Arg.Any<PageParameters>())
            .Returns(new PaginatedResult<Event>
            {
                Items = [GetStoredEvent(id: storedId, title: storedTitle)],
                Page = 2,
                PageSize = 10,
                TotalCount = 15
            });

        var result = _service.GetByFilter(
            new EventSearchCriteria(),
            [],
            new PageParameters());

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(storedId);
        result.Items[0].Title.Should().Be(storedTitle);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(15);
    }

    [Fact]
    public void GetByFilter_EmptyResult_ReturnsEmptyPaginated()
    {
        _repository
            .FindByFilter(
                Arg.Any<EventSearchCriteria>(),
                Arg.Any<IReadOnlyList<SortOrder<EventSortField>>>(),
                Arg.Any<PageParameters>())
            .Returns(new PaginatedResult<Event>
            {
                Items = [],
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            });

        var result = _service.GetByFilter(
            new EventSearchCriteria(),
            [],
            new PageParameters());

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    private static EventSaveDto GetValidDto(string title = "Концерт") => new()
    {
        Title = title,
        StartAt = DefaultStart,
        EndAt = DefaultEnd
    };

    private static Event GetStoredEvent(
        int id = 1,
        string title = "Концерт",
        DateTime? startAt = null,
        DateTime? endAt = null) => new()
        {
            Id = id,
            Title = title,
            StartAt = startAt ?? DefaultStart,
            EndAt = endAt ?? DefaultEnd
        };
}