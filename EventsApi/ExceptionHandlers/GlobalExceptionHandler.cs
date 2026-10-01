using EventsApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace EventsApi.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = MapExceptionToProblem(httpContext, exception);
        LogException(httpContext, exception);

        if (httpContext.Response.HasStarted)
            return true;

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), cancellationToken);

        return true;
    }

    private ProblemDetails MapExceptionToProblem(HttpContext context, Exception exception)
        => exception switch
        {
            DataValidationException v => BuildValidationProblem(context, v),

            AppException appEx => BuildProblem(context,
                (int)appEx.StatusCode,
                appEx.Title,
                appEx.Message),

            _ => BuildProblem(context,
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка. Пожалуйста, обратитесь в поддержку")
        };

    private ProblemDetails BuildProblem(
        HttpContext context, int statusCode, string title, string detail)
        => new()
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

    private ValidationProblemDetails BuildValidationProblem(
        HttpContext context, DataValidationException exception)
        => new(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = exception.Title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

    private void LogException(HttpContext context, Exception exception)
    {
        if (exception is AppException appException)
        {
            _logger.LogWarning(
                "Обработанное исключение {Type} на {Method} {Path}: {Message}",
                appException.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                appException.Message);
        }
        else
        {
            _logger.LogError(exception,
                "Необработанное исключение на {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
    }
}
