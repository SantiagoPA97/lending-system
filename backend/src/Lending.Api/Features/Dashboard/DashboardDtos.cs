using Lending.Domain;

namespace Lending.Api.Features.Dashboard;

public sealed record DashboardMetricsResponse(
    DateTime GeneratedAtUtc,
    IReadOnlyList<CurrencyPortfolioResponse> Portfolio,
    Dictionary<FacilityStatus, int> FacilitiesByStatus,
    IReadOnlyList<CurrencyRepaymentsResponse> RepaymentsLast30Days,
    IReadOnlyList<CurrencyInstallmentsResponse> UpcomingInstallmentsNext30Days,
    OverdueInstallmentsResponse OverdueInstallments,
    IReadOnlyList<CurrencyExposureResponse> TopCompanyExposures);

public sealed record CurrencyPortfolioResponse(
    Currency Currency,
    int FacilityCount,
    decimal TotalCommitted,
    decimal TotalOutstanding);

public sealed record CurrencyRepaymentsResponse(
    Currency Currency,
    int Count,
    decimal NetAmount);

public sealed record CurrencyInstallmentsResponse(
    Currency Currency,
    int Count,
    decimal AmountDue);

public sealed record OverdueInstallmentsResponse(
    int Count,
    DateOnly? OldestDueDate);

public sealed record CurrencyExposureResponse(
    Currency Currency,
    IReadOnlyList<CompanyExposureResponse> Companies);

public sealed record CompanyExposureResponse(
    Guid CompanyId,
    string CompanyName,
    decimal Outstanding);
