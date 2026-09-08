using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) =
            exception switch
            {
                BadHttpRequestException badRequest
                    when badRequest.StatusCode ==
                         StatusCodes
                             .Status413PayloadTooLarge =>
                    (
                        StatusCodes
                            .Status413PayloadTooLarge,
                        "Request body is too large.",
                        "Reduce the uploaded file size and try again."
                    ),
                BadHttpRequestException badRequest =>
                    (
                        badRequest.StatusCode,
                        "Invalid HTTP request.",
                        "Correct the request and try again."
                    ),
                InvalidDataException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Invalid request body.",
                        "The request body could not be read."
                    ),
                _ =>
                    (
                        StatusCodes
                            .Status500InternalServerError,
                        "An unexpected error occurred.",
                        "The server could not complete the request."
                    )
            };

        httpContext.Response.StatusCode =
            statusCode;

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request rejected with status {StatusCode}. TraceId: {TraceId}",
                statusCode,
                httpContext.TraceIdentifier);
        }

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = httpContext.Request.Path
                }
            });
    }
}
