using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PoNovaWeight.Api.Infrastructure;

/// <summary>
/// Global exception handler that converts exceptions to RFC 7807 ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage)),
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            UnauthorizedAccessException unauthorizedEx => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorizedEx.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            ArgumentException argEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = argEx.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            InvalidOperationException invalidOp => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid Operation",
                Detail = invalidOp.Message,
                Type = "https://tools.ietf.org/html/rfc7807"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Please try again later.",
                Type = "https://tools.ietf.org/html/rfc7807"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
