using EventsApi.Data.Repositories;
using EventsApi.Dtos;
using EventsApi.Exceptions;
using EventsApi.Mappers;
using EventsApi.Models;
using EventsApi.Validation;

namespace EventsApi.Services;

public class EventService : IEventService
{
    private readonly EventMapper _eventMapper;
    private readonly IEventRepository _eventRepository;
    private readonly IModelValidator _modelValidator;

    public EventService(EventMapper eventMapper, IEventRepository eventRepository, IModelValidator modelValidator)
    {
        _eventMapper = eventMapper;
        _eventRepository = eventRepository;
        _modelValidator = modelValidator;
    }

    public EventDto Create(EventSaveDto? saveDto)
    {
        ValidateSaveDtoOrThrow(saveDto);

        var model = _eventMapper.SaveDtoToModel(saveDto);

        var savedModel = _eventRepository.Add(model);
        return _eventMapper.ToDto(savedModel);
    }

    public void Delete(int id)
    {
        if (!_eventRepository.Delete(id))
            throw new NotFoundException(nameof(Event), id);
    }

    public List<EventDto> GetAll()
    {
        return _eventRepository.GetAll()
            .Select(_eventMapper.ToDto)
            .ToList();
    }

    public EventDto GetById(int id)
    {
        var model = _eventRepository.FindById(id);

        if (model is null)
            throw new NotFoundException(nameof(Event), id);
        return _eventMapper.ToDto(model);
    }

    public void Update(int id, EventSaveDto? saveDto)
    {
        ValidateSaveDtoOrThrow(saveDto);

        var model = _eventRepository.FindById(id) 
            ?? throw new NotFoundException(nameof(Event), id);

        _eventMapper.SaveDtoToModel(saveDto, model);
        _eventRepository.Update(id, model);
    }

    private void ValidateSaveDtoOrThrow(EventSaveDto? saveDto)
    {
        var errors = _modelValidator.Validate(saveDto);

        if (saveDto is not null && saveDto.EndAt <= saveDto.StartAt)
            errors.AddError(nameof(EventSaveDto.EndAt), $"{nameof(EventSaveDto.EndAt)} должен быть позже {nameof(EventSaveDto.StartAt)}");

        if (errors.HasErrors)
            throw new DataValidationException(errors);
    }
}
