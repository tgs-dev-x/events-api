using System.ComponentModel.DataAnnotations;

namespace EventsApi.Dtos;

public record EventSaveDto
{
    [Required(ErrorMessage = "Title обязателен"), MaxLength(100, ErrorMessage = "Title не должен превышать 100 символов")]
    public string? Title { get; init; }

    [MaxLength(100, ErrorMessage = "Description не должен превышать 100 символов")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "StartAt обязателен")]
    public DateTime? StartAt { get; init; }

    [Required(ErrorMessage = "EndAt обязателен")]
    public DateTime? EndAt { get; init; }
}
