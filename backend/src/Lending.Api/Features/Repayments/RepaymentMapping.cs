using Lending.Domain.Entities;

namespace Lending.Api.Features.Repayments;

public static class RepaymentMapping
{
    public static RepaymentResponse ToResponse(this Repayment repayment) => new(
        repayment.Id,
        repayment.FacilityId,
        repayment.Amount,
        repayment.Currency,
        repayment.PaymentDate,
        repayment.PrincipalApplied,
        repayment.InterestApplied,
        repayment.IsReversal,
        repayment.ReversesRepaymentId,
        repayment.ReversedByRepaymentId,
        repayment.Note,
        repayment.CreatedAtUtc,
        repayment.Allocations
            .Select(a => new RepaymentAllocationResponse(a.Period, a.Interest, a.Principal))
            .ToList());

    public static RepaymentResultResponse ToResultResponse(this Repayment repayment, Facility facility) => new(
        repayment.ToResponse(),
        repayment.InterestApplied,
        repayment.PrincipalApplied,
        facility.OutstandingPrincipal,
        facility.Status);
}
