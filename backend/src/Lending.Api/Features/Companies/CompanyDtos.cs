using Lending.Domain;

namespace Lending.Api.Features.Companies;

public sealed record CompanyRequest(
    string Name,
    string LegalName,
    string RegistrationNumber,
    string Country,
    string Industry,
    string ContactEmail);

public sealed record CompanyResponse(
    Guid Id,
    string Name,
    string LegalName,
    string RegistrationNumber,
    string Country,
    string Industry,
    string ContactEmail,
    CompanyStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CompanyDetailResponse(
    Guid Id,
    string Name,
    string LegalName,
    string RegistrationNumber,
    string Country,
    string Industry,
    string ContactEmail,
    CompanyStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<CompanyFacilitySummaryResponse> Facilities);

public sealed record CompanyFacilitySummaryResponse(
    Guid Id,
    string Reference,
    decimal CommitmentAmount,
    Currency Currency,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType,
    FacilityStatus Status,
    decimal OutstandingPrincipal);
