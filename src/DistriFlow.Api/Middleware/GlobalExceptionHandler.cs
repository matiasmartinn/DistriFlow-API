using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var clientCancelled = exception is OperationCanceledException
            && httpContext.RequestAborted.IsCancellationRequested;

        var statusCode = clientCancelled
            ? 499
            : StatusCodes.Status500InternalServerError;

        if (!clientCancelled)
        {
            _logger.LogError(
                exception,
                "Error inesperado en {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = clientCancelled ? "Solicitud cancelada" : "Error inesperado",
            Detail = clientCancelled
                ? "El cliente canceló la solicitud."
                : "Ocurrió un error inesperado.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = clientCancelled
            ? "request.cancelled"
            : "unexpected.error";
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
