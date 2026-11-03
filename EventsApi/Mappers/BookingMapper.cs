using EventsApi.Dtos;
using EventsApi.Models;
using EventsApi.Models.Enums;
using Riok.Mapperly.Abstractions;

namespace EventsApi.Mappers;

[Mapper]
public partial class BookingMapper
{
    [MapProperty(nameof(Booking.Status), nameof(BookingDto.StatusDescription))]
    public partial BookingDto ToDto(Booking model);

    private string MapStatusDescription(BookingStatus status)
        => status.GetDescription();
}
