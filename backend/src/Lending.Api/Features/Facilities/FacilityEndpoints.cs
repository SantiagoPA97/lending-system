namespace Lending.Api.Features.Facilities;

// TODO: stub — replaced entirely by the facilities feature agent.
public static class FacilityEndpoints
{
    public static RouteGroupBuilder MapFacilityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NotImplemented);
        return group;
    }

    private static IResult NotImplemented() => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Not implemented",
        detail: "Facility endpoints are not implemented yet.");
}
