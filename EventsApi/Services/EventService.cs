using EventsApi.Common.Pagination;
using EventsApi.Common.Sorting;
using EventsApi.Data.Repositories;
using EventsApi.Dtos;
using EventsApi.Exceptions;
using EventsApi.Filtering;
using EventsApi.Mappers;
using EventsApi.Models;
using EventsApi.Validation;

namespace EventsApi.Services;

public class EventService : IEventService
{
    private readonly EventMapper _eventMapper;
    private readonly IEventRepository _eventRepository;
    private readonly IModelValidator _modelValidator;

    private readonly BookingSynchronization _synchronization;

    public EventService(EventMapper eventMapper, IEventRepository eventRepository, IModelValidator modelValidator, BookingSynchronization synchronization)
    {
        _eventMapper = eventMapper;
        _eventRepository = eventRepository;
        _modelValidator = modelValidator;
        _synchronization = synchronization;
    }

    public EventDto Create(EventSaveDto? saveDto)
    {
        ValidateSaveDtoOrThrow(saveDto);

        var model = _eventMapper.SaveDtoToModel(saveDto);

        var savedModel = _eventRepository.Add(model);
        return _eventMapper.ToDto(savedModel);
    }

    public void Delete(Guid id)
    {
        _synchronization.Gate.Wait();
        try
        {
            if (!_eventRepository.Delete(id))
                throw new NotFoundException(nameof(Event), id);
        }
        finally
        {
            _synchronization.Gate.Release();
        }
    }

    public PaginatedResult<EventDto> GetByFilter(EventSearchCriteria criteria,
        IReadOnlyList<SortOrder<EventSortField>> sorts,
        PageParameters pageParameters)
    {
        var pagedResult = _eventRepository.FindByFilter(criteria, sorts, pageParameters);

        var items = pagedResult.Items;
        var dtos = items.Select(_eventMapper.ToDto)
            .ToList();

        return new PaginatedResult<EventDto>
        {
            Items = dtos,
            Page = pagedResult.Page,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount
        };
    }

    public EventDto GetById(Guid id)
    {
        var model = _eventRepository.FindById(id);

        if (model is null)
            throw new NotFoundException(nameof(Event), id);
        return _eventMapper.ToDto(model);
    }

    public void Update(Guid id, EventSaveDto? saveDto)
    {
        ValidateSaveDtoOrThrow(saveDto);

        _synchronization.Gate.Wait();
        try
        {
            var model = _eventRepository.FindById(id)
                ?? throw new NotFoundException(nameof(Event), id);

            model.ChangeTotalSeats(saveDto!.TotalSeats!.Value);
            _eventMapper.SaveDtoToModel(saveDto, model);
            _eventRepository.Update(id, model);
        }
        finally
        {
            _synchronization.Gate.Release();
        }
    }

    private void ValidateSaveDtoOrThrow(EventSaveDto? saveDto)
    {
        var errors = _modelValidator.Validate(saveDto);

        if (saveDto is not null)
        {
            if (saveDto.EndAt <= saveDto.StartAt)
                errors.AddError(
                    nameof(EventSaveDto.EndAt),
                    $"{nameof(EventSaveDto.EndAt)} должен быть позже {nameof(EventSaveDto.StartAt)}");

            if (saveDto.TotalSeats <= 0)
                errors.AddError(
                    nameof(EventSaveDto.TotalSeats),
                    $"{nameof(EventSaveDto.TotalSeats)} должен быть больше 0");
        }

        if (errors.HasErrors)
            throw new DataValidationException(errors);
    }
}
