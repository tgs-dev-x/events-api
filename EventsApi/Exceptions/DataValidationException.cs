using EventsApi.Validation;
using System.Net;

namespace EventsApi.Exceptions;

public sealed class DataValidationException : AppException
{
    private const string DefaultTitle = "Ошибка валидации";
    private const string DefaultMessage = "Ошибка валидации входных данных.";

    public IDictionary<string, string[]> Errors { get; }

    public DataValidationException(string message, ValidationErrors errors)
        : base(DefaultTitle, message, HttpStatusCode.BadRequest)
    {
        Errors = errors.ToDictionary();
    }

    public DataValidationException(ValidationErrors errors)
        : this(DefaultMessage, errors) { }

    public DataValidationException(string message)
        : this(message, new ValidationErrors()) { }
}
