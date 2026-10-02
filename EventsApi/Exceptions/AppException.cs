using System.Net;

namespace EventsApi.Exceptions;

public abstract class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string Title { get; }

    protected AppException(string title, string message, HttpStatusCode statusCode)
        : base(message)
    {
        Title = title;
        StatusCode = statusCode;
    }
}
