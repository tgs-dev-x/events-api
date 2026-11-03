using EventsApi.Dtos;
using EventsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.Controllers;

[ApiController]
[Route("bookings")]
[Tags("Бронирования")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Создать бронь для мероприятия
    /// </summary>
    [HttpPost("/events/{eventId:guid}/book")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> Create(Guid eventId)
    {
        var booking = await _bookingService.CreateBookingAsync(eventId);

        return AcceptedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = booking.Id },
            value: booking);
    }

    /// <summary>
    /// Получить бронь по идентификатору
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> GetById(Guid id)
    {
        return Ok(await _bookingService.GetBookingByIdAsync(id));
    }
}
