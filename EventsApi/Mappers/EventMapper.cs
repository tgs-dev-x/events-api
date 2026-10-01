using EventsApi.Dtos;
using EventsApi.Models;
using Riok.Mapperly.Abstractions;

namespace EventsApi.Mappers;

[Mapper]
public partial class EventMapper
{
    public partial EventDto ToDto(Event model);

    [MapperIgnoreTarget(nameof(Event.Id))]
    public partial Event SaveDtoToModel(EventSaveDto? saveDto);

    [MapperIgnoreTarget(nameof(Event.Id))]
    public partial void SaveDtoToModel(EventSaveDto? saveDto, Event model);
    
}
