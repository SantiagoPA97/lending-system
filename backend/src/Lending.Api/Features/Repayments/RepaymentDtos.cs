using Lending.Domain;

namespace Lending.Api.Features.Repayments;

public sealed record RecordRepaymentRequest(
    decimal Amount,
    Currency Currency,
    DateOnly PaymentDate,
    string? Note);

public sealed record ReverseRepaymentRequest(DateOnly? ReversalDate);

public sealed record RepaymentAllocationResponse(int Period, decimal Interest, decimal Principal);

public sealed record RepaymentResponse(
    Guid Id,
    Guid FacilityId,
    decimal Amount,
    Currency Currency,
    DateOnly PaymentDate,
    decimal PrincipalApplied,
    decimal InterestApplied,
    bool IsReversal,
    Guid? ReversesRepaymentId,
    Guid? ReversedByRepaymentId,
    string? Note,
    DateTime CreatedAtUtc,
    IReadOnlyList<RepaymentAllocationResponse> Allocations);

public sealed record RepaymentResultResponse(
    RepaymentResponse Repayment,
    decimal InterestPaid,
    decimal PrincipalPaid,
    decimal NewOutstanding,
    FacilityStatus FacilityStatus);
