using DistriFlow.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace DistriFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected IActionResult HandleResult<T>(
        Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsFailure)
            return MapError(result.Error);

        return StatusCode(successStatusCode, result.Data);
    }

    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return MapError(result.Error);
    }

    protected IActionResult HandleCreated<T>(
        Result<T> result,
        string actionName,
        object routeValues)
    {
        if (result.IsFailure)
            return MapError(result.Error);

        return CreatedAtAction(actionName, routeValues, result.Data);
    }

    private IActionResult MapError(Error error)
    {
        var (statusCode, title, type) = error.Type switch
        {
            ErrorType.Validation => (
                StatusCodes.Status400BadRequest,
                "Error de validación",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),

            ErrorType.Domain => (
                StatusCodes.Status400BadRequest,
                "Regla de negocio violada",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),

            ErrorType.Unauthorized => (
                StatusCodes.Status401Unauthorized,
                "No autenticado",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2"),

            ErrorType.Forbidden => (
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"),

            ErrorType.NotFound => (
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"),

            ErrorType.Conflict => (
                StatusCodes.Status409Conflict,
                "Conflicto",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Error inesperado",
                "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = error.Type == ErrorType.Unexpected
                ? "Ocurrió un error inesperado."
                : error.Message,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["code"] = error.Code;
        problemDetails.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return StatusCode(statusCode, problemDetails);
    }
}
