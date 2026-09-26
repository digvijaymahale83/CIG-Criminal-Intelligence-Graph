using System.Net;
using System.Text.Json;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("D");

        _logger.LogError(exception, "Unhandled exception occurred during request execution. CorrelationId: {CorrelationId}", correlationId);

        var statusCode = HttpStatusCode.InternalServerError;
        var title = "An unexpected error occurred while processing the investigative request.";
        var detail = "Please reference the correlation ID with technical operations.";

        if (exception is EntityNotFoundException notFoundEx)
        {
            statusCode = HttpStatusCode.NotFound;
            title = "Investigative Resource Not Found";
            detail = notFoundEx.Message;
        }
        else if (exception is BusinessRuleValidationException validationEx)
        {
            statusCode = HttpStatusCode.BadRequest;
            title = "Business Rule Validation Failed";
            detail = validationEx.Message;
        }
        else if (exception is SystemDegradedException degradedEx)
        {
            statusCode = HttpStatusCode.ServiceUnavailable;
            title = "System Service Degraded";
            detail = degradedEx.Message;
        }
        else if (_env.IsDevelopment())
        {
            detail = exception.Message;
        }

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestampUtc"] = DateTime.UtcNow.ToString("o");

        context.Response.Clear();
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
