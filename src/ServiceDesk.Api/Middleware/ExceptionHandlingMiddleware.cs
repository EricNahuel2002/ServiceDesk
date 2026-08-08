using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Exceptions;

namespace ServiceDesk.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            LogException(context, exception);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "The response already started, skipping error response for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                return;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private void LogException(HttpContext context, Exception exception)
    {
        if (exception is ValidationException or UnauthorizedException or NotFoundException)
        {
            _logger.LogWarning(
                exception,
                "Request failed while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            return;
        }

        _logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            context.Request.Method,
            context.Request.Path);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (int Status, ProblemDetails Details) result = exception switch
        {
            ValidationException validation => CreateValidationError(context, validation),
            UnauthorizedException unauthorized => CreateProblem(
                context,
                StatusCodes.Status401Unauthorized,
                "No autorizado.",
                unauthorized.Message),
            NotFoundException notFound => CreateProblem(
                context,
                StatusCodes.Status404NotFound,
                "No encontrado.",
                notFound.Message),
            _ => CreateProblem(
                context,
                StatusCodes.Status500InternalServerError,
                "An error occurred while processing your request.",
                _environment.IsDevelopment() ? exception.Message : null)
        };

        context.Response.StatusCode = result.Status;
        context.Response.ContentType = "application/problem+json";

        try
        {
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(result.Details, result.Details.GetType(), JsonOptions));
        }
        catch (Exception writeException)
        {
            _logger.LogError(
                writeException,
                "Failed to write error response for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
    }

    private static (int Status, ProblemDetails Details) CreateValidationError(
        HttpContext context,
        ValidationException exception)
    {
        var details = new ValidationProblemDetails(exception.Errors.ToDictionary(e => e.Key, e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Se produjeron errores de validación.",
            Instance = context.Request.Path
        };

        return (StatusCodes.Status400BadRequest, details);
    }

    private static (int Status, ProblemDetails Details) CreateProblem(
        HttpContext context,
        int status,
        string title,
        string? detail)
    {
        var details = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        return (status, details);
    }
}
