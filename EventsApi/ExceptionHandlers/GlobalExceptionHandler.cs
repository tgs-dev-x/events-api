using EventsApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventsApi.ExceptionHandlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = MapExceptionToProblem(httpContext, exception);

        LogException(httpContext, exception);

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    private ProblemDetails MapExceptionToProblem(HttpContext context, Exception exception)
        => exception switch
        {
            DataValidationException v => BuildValidationProblem(context, v),

            AppException appEx => BuildProblem(context,
                (int)appEx.StatusCode,
                GetProblemTitle(appEx),
                appEx.Message),

            _ => BuildProblem(context,
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Внутренняя ошибка сервера")
        };

    private string GetProblemTitle(AppException exception) => exception switch
    {
        NotFoundException => "Ресурс не найден",
        _ => "Внутренняя ошибка сервера"
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
            Title = "Ошибка валидации",
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
