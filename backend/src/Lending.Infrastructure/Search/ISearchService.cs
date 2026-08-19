using Lending.Domain;

namespace Lending.Infrastructure.Search;

public interface ISearchService
{
    Task<SearchResultSet> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken);
}

public static class SearchResultTypes
{
    public const string Company = "company";
    public const string Facility = "facility";
}

public sealed record SearchCriteria(
    string? Query,
    bool IncludeCompanies,
    bool IncludeFacilities,
    CompanyStatus? CompanyStatus,
    FacilityStatus? FacilityStatus,
    RepaymentType? RepaymentType,
    Currency? Currency,
    decimal? MinAmount,
    decimal? MaxAmount,
    int Page,
    int PageSize);

public sealed record SearchHit(
    string Type,
    Guid Id,
    string Name,
    string CompanyName,
    string Status,
    RepaymentType? RepaymentType,
    Currency? Currency,
    decimal? Amount,
    decimal? Outstanding,
    double Rank);

public sealed record SearchResultSet(IReadOnlyList<SearchHit> Hits, int Total);
