using Lending.Domain.Entities;

namespace Lending.Api.Features.Companies;

public static class CompanyMapping
{
    public static CompanyResponse ToResponse(this Company company) => new(
        company.Id,
        company.Name,
        company.LegalName,
        company.RegistrationNumber,
        company.Country,
        company.Industry,
        company.ContactEmail,
        company.Status,
        company.CreatedAtUtc,
        company.UpdatedAtUtc);

    public static CompanyDetailResponse ToDetailResponse(
        this Company company,
        IEnumerable<Facility> facilities) => new(
        company.Id,
        company.Name,
        company.LegalName,
        company.RegistrationNumber,
        company.Country,
        company.Industry,
        company.ContactEmail,
        company.Status,
        company.CreatedAtUtc,
        company.UpdatedAtUtc,
        facilities.Select(f => f.ToSummaryResponse()).ToList());

    public static CompanyFacilitySummaryResponse ToSummaryResponse(this Facility facility) => new(
        facility.Id,
        facility.Reference,
        facility.CommitmentAmount,
        facility.Currency,
        facility.AnnualInterestRate,
        facility.TermMonths,
        facility.StartDate,
        facility.RepaymentType,
        facility.Status,
        facility.OutstandingPrincipal);
}
