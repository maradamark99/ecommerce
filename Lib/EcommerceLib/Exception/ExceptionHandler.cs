using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EcommerceLib.Exception;

public class ExceptionHandler(
    ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, System.Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            HttpRequestException httpRequestException => new ProblemDetails
            {
                Status = (int?) httpRequestException.StatusCode,
                Title = "HTTP Request Error",
                Detail = httpRequestException.Message
            },
            NotFoundException notFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFoundException.Message
            },
            BadRequestException badRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = badRequestException.Message
            },
            UnauthorizedException unauthorizedException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorizedException.Message
            },
            ForbiddenException forbiddenException => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden, Title = "Forbidden",
                Detail = forbiddenException.Message
            },
            ConflictException conflictException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict", 
                Detail = conflictException.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError, Title = "Internal Server Error",
            }
        };

        logger.LogError(
            exception,
            "Exception occurred: {Message}",
            exception.Message);

        if (problemDetails.Status != null) httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}