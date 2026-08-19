using System.Text.Json;
using FluentValidation;
using Lending.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Lending.Api.Common;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            var problem = MapToProblem(exception);
            problem.Instance = context.Request.Path;
            if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
                problem.Extensions["correlationId"] = correlationId;

            if (problem.Status >= 500)
                logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Request {Method} {Path} failed with {Status}: {Message}",
                    context.Request.Method, context.Request.Path, problem.Status, exception.Message);

            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(
                problem,
                options: null,
                contentType: "application/problem+json");
        }
    }

    private ProblemDetails MapToProblem(Exception exception) => exception switch
    {
        DomainException domainException => new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Domain rule violated",
            Detail = domainException.Message,
            Extensions = { ["errorCode"] = domainException.ErrorCode }
        },
        ValidationException validationException => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = "One or more validation errors occurred.",
            Extensions =
            {
                ["errors"] = validationException.Errors
                    .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray())
            }
        },
        DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrency conflict",
            Detail = "The resource was modified by another request. Reload it and retry."
        },
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "A resource with the same unique value already exists."
            },
        EntityNotFoundException or KeyNotFoundException => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
            Detail = exception.Message
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal server error",
            Detail = environment.IsDevelopment()
                ? exception.ToString()
                : "An unexpected error occurred."
        }
    };
}
