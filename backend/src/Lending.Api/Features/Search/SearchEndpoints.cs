namespace Lending.Api.Features.Search;

// TODO: stub — replaced entirely by the search feature agent.
public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NotImplemented);
        return group;
    }

    private static IResult NotImplemented() => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Not implemented",
        detail: "Search endpoints are not implemented yet.");
}
