using Lending.Domain;

namespace Lending.Api.Features.Repayments;

// TODO: Note is accepted for API-contract stability but not yet persisted — the Repayment
// entity has no note column; adding it needs a domain field + migration.
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
    DateTime CreatedAtUtc,
    IReadOnlyList<RepaymentAllocationResponse> Allocations);

public sealed record RepaymentResultResponse(
    RepaymentResponse Repayment,
    decimal InterestPaid,
    decimal PrincipalPaid,
    decimal NewOutstanding,
    FacilityStatus FacilityStatus);
