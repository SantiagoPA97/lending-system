using Lending.Api.Common;
using Lending.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lending.Api.Features.Assistant;

public static class AssistantEndpoints
{
    public static RouteGroupBuilder MapAssistantEndpoints(this RouteGroupBuilder group)
    {
        // Read-only feature: the group-level Viewer policy is sufficient.
        group.MapPost("/query", Query).WithValidation<AssistantQueryRequest>();
        return group;
    }

    private static async Task<IResult> Query(
        AssistantQueryRequest request,
        LendingDbContext db,
        HybridCache cache,
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Lending.Api.Features.Assistant");
        try
        {
            var response = await AssistantService.QueryAsync(
                request, db, cache, configuration, logger, cancellationToken);
            return Results.Ok(response);
        }
        catch (AssistantUpstreamException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Assistant unavailable",
                detail: ex.Message,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "assistant.upstream_error" });
        }
    }
}
