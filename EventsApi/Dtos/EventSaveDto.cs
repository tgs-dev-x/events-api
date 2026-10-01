using System.ComponentModel.DataAnnotations;

namespace EventsApi.Dtos;

public record EventSaveDto
{
    [Required(ErrorMessage = "Title обязателен")]
    public string? Title { get; init; }

    public string? Description { get; init; }

    [Required(ErrorMessage = "StartAt обязателен")]
    public DateTime? StartAt { get; init; }

    [Required(ErrorMessage = "EndAt обязателен")]
    public DateTime? EndAt { get; init; }
}
