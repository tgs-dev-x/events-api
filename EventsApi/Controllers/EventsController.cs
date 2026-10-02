using EventsApi.Dtos;
using EventsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EventsApi.Controllers;

[ApiController]
[Route("events")]
[Tags("Мероприятия")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Создать мероприятие
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<EventDto> Create([FromBody] [ValidateNever] EventSaveDto saveDto)
    {
        var created = _eventService.Create(saveDto);

        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = created.Id },
            value: created);
    }

    /// <summary>
    /// Получить мероприятие по идентификатору
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<EventDto> GetById(int id)
    {
        return Ok(_eventService.GetById(id));
    }

    /// <summary>
    /// Получить список мероприятий
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public ActionResult<List<EventDto>> GetAll()
    {
        return Ok(_eventService.GetAll());
    }

    /// <summary>
    /// Обновить мероприятие
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Update(int id, [FromBody] [ValidateNever] EventSaveDto saveDto)
    {
        _eventService.Update(id, saveDto);
        return NoContent();
    }

    /// <summary>
    /// Удалить мероприятие
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id)
    {
        _eventService.Delete(id);
        return NoContent();
    }
}
