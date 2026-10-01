using System.Net;

namespace EventsApi.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) 
        : base(message, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string entityName, object id)
        : base($"{entityName} с id '{id}' не найден.", HttpStatusCode.NotFound)
    {
    }
}
