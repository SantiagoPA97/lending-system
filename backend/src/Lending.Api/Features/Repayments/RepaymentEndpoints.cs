using Lending.Api.Common;
using Lending.Api.Features.Auth;
using Lending.Domain.Entities;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Features.Repayments;

public static class RepaymentEndpoints
{
    public static RouteGroupBuilder MapRepaymentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/reverse", Reverse)
            .RequireAuthorization(AuthPolicies.Admin);
        return group;
    }

    private static async Task<IResult> Reverse(
        Guid id,
        ReverseRepaymentRequest? request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var facility = await db.Facilities
                           .Include(f => f.Schedule)
                           .Include(f => f.Repayments)
                           .FirstOrDefaultAsync(f => f.Repayments.Any(r => r.Id == id), cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Repayment), id);

        var reversalDate = request?.ReversalDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var reversal = facility.ReverseRepayment(id, reversalDate);
        db.Repayments.Add(reversal);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(reversal.ToResultResponse(facility));
    }
}
