using System.Net;

namespace EventsApi.Exceptions;

public sealed class NotFoundException : AppException
{
    private const string DefaultTitle = "Ресурс не найден";

    public NotFoundException(string message) 
        : base(DefaultTitle, message, HttpStatusCode.NotFound)
    {
    }

    public NotFoundException(string entityName, object id)
        : this($"{entityName} с id '{id}' не найден.")
    {
    }
}
