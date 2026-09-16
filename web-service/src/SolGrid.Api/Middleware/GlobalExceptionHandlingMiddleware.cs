/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: GlobalExceptionHandlingMiddleware.cs
 * Description: Converts application exceptions into consistent API problem responses.
 * Contributor: Dilshan Yapa
 */

using Microsoft.AspNetCore.Mvc;
using SolGrid.Application.Common.Exceptions;

namespace SolGrid.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        // Capture middleware dependencies for request execution and error logging.
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Execute the request and translate known exceptions into problem responses.
        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (ValidationException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                exception.Errors).ConfigureAwait(false);
        }
        catch (AuthenticationFailedException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                exception.Message).ConfigureAwait(false);
        }
        catch (AccountInactiveException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                exception.Message).ConfigureAwait(false);
        }
        catch (ForbiddenException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status403Forbidden,
                exception.Message).ConfigureAwait(false);
        }
        catch (NotFoundException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status404NotFound,
                exception.Message).ConfigureAwait(false);
        }
        catch (ConflictException exception)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                exception.Message).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Hide unexpected error details from API clients while preserving server logs.
            logger.LogError(exception, "An unhandled API exception occurred.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.").ConfigureAwait(false);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        IReadOnlyList<string>? errors = null)
    {
        // Write a consistent JSON problem response.
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        await context.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json")
            .ConfigureAwait(false);
    }
}
