using Asp.Versioning;
using EventsApi.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Tags("Мероприятия")]
public class EventsController : ControllerBase
{
    private readonly EventMapper _mapper;

    public EventsController(EventMapper mapper)
    {
        _mapper = mapper;
    }

  
}
