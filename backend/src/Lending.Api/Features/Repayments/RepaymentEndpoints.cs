namespace Lending.Api.Features.Repayments;

// TODO: stub — replaced entirely by the repayments feature agent.
public static class RepaymentEndpoints
{
    public static RouteGroupBuilder MapRepaymentEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", NotImplemented);
        return group;
    }

    private static IResult NotImplemented() => Results.Problem(
        statusCode: StatusCodes.Status501NotImplemented,
        title: "Not implemented",
        detail: "Repayment endpoints are not implemented yet.");
}
