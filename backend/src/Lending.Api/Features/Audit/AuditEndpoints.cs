namespace Lending.Api.Features.Audit;

// TODO: stub — replaced entirely by the audit feature agent.
public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NotImplemented);
        return group;
    }

    private static IResult NotImplemented() => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Not implemented",
        detail: "Audit endpoints are not implemented yet.");
}
