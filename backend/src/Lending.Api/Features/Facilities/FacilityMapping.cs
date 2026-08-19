using Lending.Domain;
using Lending.Domain.Entities;

namespace Lending.Api.Features.Facilities;

public static class FacilityMapping
{
    public static FacilityResponse ToResponse(this Facility facility, string companyName) => new(
        facility.Id,
        facility.Reference,
        facility.CompanyId,
        companyName,
        facility.CommitmentAmount,
        facility.Currency,
        facility.AnnualInterestRate,
        facility.TermMonths,
        facility.StartDate,
        facility.RepaymentType,
        facility.Status,
        facility.OutstandingPrincipal,
        facility.CreatedAtUtc,
        facility.UpdatedAtUtc);

    public static FacilityDetailResponse ToDetailResponse(
        this Facility facility,
        Company company,
        IReadOnlyList<RepaymentScheduleItem> schedule)
    {
        var nextDue = schedule
            .Where(i => !i.IsSettled)
            .OrderBy(i => i.Period)
            .FirstOrDefault();

        return new FacilityDetailResponse(
            facility.Id,
            facility.Reference,
            new FacilityCompanySummaryResponse(
                company.Id,
                company.Name,
                company.Status,
                company.Country,
                company.Industry),
            facility.CommitmentAmount,
            facility.Currency,
            facility.AnnualInterestRate,
            facility.TermMonths,
            facility.StartDate,
            facility.RepaymentType,
            facility.Status,
            facility.OutstandingPrincipal,
            schedule.Sum(i => i.PrincipalPaid),
            schedule.Sum(i => i.InterestPaid),
            nextDue?.ToItemResponse(),
            facility.CreatedAtUtc,
            facility.UpdatedAtUtc);
    }

    public static ScheduleItemResponse ToItemResponse(this RepaymentScheduleItem item) => new(
        item.Period,
        item.DueDate,
        item.PrincipalDue,
        item.InterestDue,
        item.PrincipalDue + item.InterestDue,
        item.RemainingBalance,
        item.PrincipalPaid,
        item.InterestPaid,
        item.IsSettled);

    public static SchedulePreviewInstallmentResponse ToPreviewResponse(this SchedulePeriod period) => new(
        period.Period,
        period.DueDate,
        period.PrincipalDue,
        period.InterestDue,
        period.PrincipalDue + period.InterestDue,
        period.RemainingBalance);
}
