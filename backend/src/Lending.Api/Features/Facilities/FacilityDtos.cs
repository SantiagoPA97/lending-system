using Lending.Domain;

namespace Lending.Api.Features.Facilities;

public interface IFacilityTermsRequest
{
    decimal CommitmentAmount { get; }
    Currency Currency { get; }
    decimal AnnualInterestRate { get; }
    int TermMonths { get; }
    DateOnly StartDate { get; }
    RepaymentType RepaymentType { get; }
}

public sealed record CreateFacilityRequest(
    Guid CompanyId,
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType) : IFacilityTermsRequest;

public sealed record UpdateFacilityRequest(
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType) : IFacilityTermsRequest;

public sealed record SchedulePreviewRequest(
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType) : IFacilityTermsRequest;

public sealed record FacilityResponse(
    Guid Id,
    string Reference,
    Guid CompanyId,
    string CompanyName,
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType,
    FacilityStatus Status,
    decimal OutstandingPrincipal,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record FacilityCompanySummaryResponse(
    Guid Id,
    string Name,
    CompanyStatus Status,
    string Country,
    string Industry);

public sealed record FacilityDetailResponse(
    Guid Id,
    string Reference,
    FacilityCompanySummaryResponse Company,
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType,
    FacilityStatus Status,
    decimal OutstandingPrincipal,
    decimal TotalPrincipalPaid,
    decimal TotalInterestPaid,
    ScheduleItemResponse? NextDueInstallment,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ScheduleItemResponse(
    int Period,
    DateOnly DueDate,
    decimal PrincipalDue,
    decimal InterestDue,
    decimal TotalDue,
    decimal RemainingBalance,
    decimal PrincipalPaid,
    decimal InterestPaid,
    bool IsSettled);

public sealed record ScheduleResponse(
    Guid FacilityId,
    string Reference,
    Currency Currency,
    IReadOnlyList<ScheduleItemResponse> Items,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalPayable);

public sealed record SchedulePreviewInstallmentResponse(
    int Period,
    DateOnly DueDate,
    decimal PrincipalDue,
    decimal InterestDue,
    decimal TotalDue,
    decimal RemainingBalance);

public sealed record SchedulePreviewResponse(
    Currency Currency,
    IReadOnlyList<SchedulePreviewInstallmentResponse> Installments,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalPayable);
