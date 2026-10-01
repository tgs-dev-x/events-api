using System.Net;

namespace EventsApi.Exceptions;

public sealed class DataValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public DataValidationException(string message, IDictionary<string, string[]> errors)
        : base(message, HttpStatusCode.BadRequest) 
    {
        Errors = errors;
    }

    public DataValidationException(string message)
    : this(message, new Dictionary<string, string[]>()) 
    {
    }
}
