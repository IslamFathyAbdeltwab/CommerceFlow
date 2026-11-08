using CommerceFlow.Application.Common.Exceptions;
using CommerceFlow.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CommerceFlow.API.Common;

// Boundary between Application exceptions and HTTP. Handlers and Domain stay
// ignorant of StatusCodes: they throw, the middleware maps to the right 4xx/5xx.
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            await WriteValidationAsync(context, ex);
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title = ReasonFor(statusCode),
            statusCode,
            detail
        });
    }

    private static async Task WriteValidationAsync(HttpContext context, FluentValidation.ValidationException ex)
    {
        if (context.Response.HasStarted)
            return;

        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/400",
            title = "Validation failed",
            statusCode = 400,
            detail = "One or more validation errors occurred.",
            errors
        });
    }

    private static string ReasonFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal Server Error"
    };
}

// Small helper: validates route/query model with FluentValidation rules
// that only exist inside the Application layer (validators for query shape).
public static class ControllerResults
{
    public static IActionResult ValidationProblem(object errors)
        => new BadRequestObjectResult(new
        {
            type = "https://httpstatuses.com/400",
            title = "Validation failed",
            statusCode = 400,
            errors
        });
}
