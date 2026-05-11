using CloudRestaurent.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Common;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("Handled {ExceptionType}: {Message}",
                exception.GetType().Name, exception.Message);

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = environment.IsDevelopment() || status < 500 ? detail : "An unexpected error occurred.",
            Instance = httpContext.Request.Path
        };

        if (exception is Application.Common.Exceptions.ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        if (environment.IsDevelopment() && status >= 500)
            problem.Extensions["exception"] = exception.GetType().FullName;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception ex) => ex switch
    {
        Application.Common.Exceptions.ValidationException v =>
            (StatusCodes.Status400BadRequest, "Validation failed", v.Message),
        NotFoundException n =>
            (StatusCodes.Status404NotFound, "Resource not found", n.Message),
        UnauthorizedException u =>
            (StatusCodes.Status401Unauthorized, "Unauthorized", u.Message),
        ForbiddenException f =>
            (StatusCodes.Status403Forbidden, "Forbidden", f.Message),
        ConflictException c =>
            (StatusCodes.Status409Conflict, "Conflict", c.Message),
        BusinessRuleException b =>
            (StatusCodes.Status422UnprocessableEntity, "Business rule violation", b.Message),
        OperationCanceledException =>
            (StatusCodes.Status499ClientClosedRequest, "Request cancelled", "Client closed the request."),
        _ =>
            (StatusCodes.Status500InternalServerError, "Server error", ex.Message)
    };
}
