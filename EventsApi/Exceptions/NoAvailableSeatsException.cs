using System.Net;

namespace EventsApi.Exceptions;

public class NoAvailableSeatsException : AppException
{
    private const string DefaultTitle = "Нет свободных мест";

    public NoAvailableSeatsException(string message)
        : base(DefaultTitle, message, HttpStatusCode.Conflict)
    {
    }
}
