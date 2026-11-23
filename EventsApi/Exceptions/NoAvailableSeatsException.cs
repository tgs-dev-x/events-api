using System.Net;

namespace EventsApi.Exceptions;

public class NoAvailableSeatsException : AppException
{
    private const string DefaultTitle = "No available seats for this event";

    public NoAvailableSeatsException(string message)
        : base(DefaultTitle, message, HttpStatusCode.Conflict)
    {
    }
}
