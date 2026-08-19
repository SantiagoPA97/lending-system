namespace Lending.Api.Features.Dashboard;

// TODO: stub — replaced entirely by the dashboard feature agent.
public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/metrics", NotImplemented);
        return group;
    }

    private static IResult NotImplemented() => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Not implemented",
        detail: "Dashboard endpoints are not implemented yet.");
}
