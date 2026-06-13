using DistriFlow.Domain.Common.Errors;
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
        var concurrencyConflict = exception is ConcurrencyException;

        var statusCode = clientCancelled
            ? 499
            : concurrencyConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status500InternalServerError;

        if (concurrencyConflict)
        {
            _logger.LogWarning(
                exception,
                "Conflicto de concurrencia en {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else if (!clientCancelled)
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
            Title = clientCancelled
                ? "Solicitud cancelada"
                : concurrencyConflict
                    ? "Conflicto"
                    : "Error inesperado",
            Detail = clientCancelled
                ? "El cliente cancelo la solicitud."
                : concurrencyConflict
                    ? "El recurso fue modificado por otra operacion. Actualiza los datos e intenta nuevamente."
                    : "Ocurrio un error inesperado.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = clientCancelled
            ? "request.cancelled"
            : concurrencyConflict
                ? "concurrency.conflict"
                : "unexpected.error";
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
