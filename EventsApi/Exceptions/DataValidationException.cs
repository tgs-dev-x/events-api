using EventsApi.Validation;
using System.Net;

namespace EventsApi.Exceptions;

public sealed class DataValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public DataValidationException(string message, ValidationErrors errors)
        : base(message, HttpStatusCode.BadRequest)
    {
        Errors = errors.ToDictionary();
    }

    public DataValidationException(ValidationErrors errors)
        : this("Ошибка валидации", errors)
    {
    }

    public DataValidationException(string message)
    : this(message, new ValidationErrors())
    {
    }
}
