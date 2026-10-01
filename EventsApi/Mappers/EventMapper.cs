using EventsApi.Dtos;
using EventsApi.Models;
using Riok.Mapperly.Abstractions;

namespace EventsApi.Mappers;

[Mapper]
public partial class EventMapper
{
    public partial EventDto ToDto(Event model);
    
}
