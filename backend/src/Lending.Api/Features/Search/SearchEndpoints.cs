using FluentValidation;
using FluentValidation.Results;
using Lending.Api.Common;
using Lending.Domain;
using Lending.Infrastructure.Search;

namespace Lending.Api.Features.Search;

public static class SearchEndpoints
{
    public static RouteGroupBuilder MapSearchEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", Search);
        return group;
    }

    private static async Task<IResult> Search(
        ISearchService searchService,
        CancellationToken cancellationToken,
        string? q = null,
        string? type = null,
        string? status = null,
        string? repaymentType = null,
        string? currency = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1)
            throw Invalid("page", "page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw Invalid("pageSize", "pageSize must be between 1 and 100.");

        var (includeCompanies, includeFacilities) = (type ?? "all").Trim().ToLowerInvariant() switch
        {
            "all" or "" => (true, true),
            "company" => (true, false),
            "facility" => (false, true),
            _ => throw Invalid("type", "type must be one of: company, facility, all.")
        };

        var query = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        if (query?.Length > 200)
            throw Invalid("q", "q must be at most 200 characters.");

        RepaymentType? repaymentTypeFilter = null;
        if (!string.IsNullOrWhiteSpace(repaymentType))
        {
            if (!Enum.TryParse<RepaymentType>(repaymentType.Trim(), ignoreCase: true, out var parsed))
                throw Invalid("repaymentType",
                    $"repaymentType must be one of: {string.Join(", ", Enum.GetNames<RepaymentType>())}.");
            repaymentTypeFilter = parsed;
        }

        Currency? currencyFilter = null;
        if (!string.IsNullOrWhiteSpace(currency))
        {
            if (!Enum.TryParse<Currency>(currency.Trim(), ignoreCase: true, out var parsed))
                throw Invalid("currency",
                    $"currency must be one of: {string.Join(", ", Enum.GetNames<Currency>())}.");
            currencyFilter = parsed;
        }

        if (minAmount is < 0m)
            throw Invalid("minAmount", "minAmount cannot be negative.");
        if (maxAmount is < 0m)
            throw Invalid("maxAmount", "maxAmount cannot be negative.");
        if (minAmount is not null && maxAmount is not null && minAmount > maxAmount)
            throw Invalid("maxAmount", "maxAmount must be greater than or equal to minAmount.");

        // Facility-specific filters narrow the search to facilities only.
        if (repaymentTypeFilter is not null || currencyFilter is not null
            || minAmount is not null || maxAmount is not null)
            includeCompanies = false;

        CompanyStatus? companyStatus = null;
        FacilityStatus? facilityStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            var isCompanyStatus = Enum.TryParse<CompanyStatus>(value, ignoreCase: true, out var parsedCompany);
            var isFacilityStatus = Enum.TryParse<FacilityStatus>(value, ignoreCase: true, out var parsedFacility);
            if (!isCompanyStatus && !isFacilityStatus)
            {
                var allowed = Enum.GetNames<CompanyStatus>().Union(Enum.GetNames<FacilityStatus>());
                throw Invalid("status", $"status must be one of: {string.Join(", ", allowed)}.");
            }

            if (isCompanyStatus)
                companyStatus = parsedCompany;
            else
                includeCompanies = false;

            if (isFacilityStatus)
                facilityStatus = parsedFacility;
            else
                includeFacilities = false;
        }

        var result = await searchService.SearchAsync(
            new SearchCriteria(
                query,
                includeCompanies,
                includeFacilities,
                companyStatus,
                facilityStatus,
                repaymentTypeFilter,
                currencyFilter,
                minAmount,
                maxAmount,
                page,
                pageSize),
            cancellationToken);

        return Results.Ok(new PagedResult<SearchResultResponse>(
            result.Hits.Select(h => h.ToResponse()).ToList(),
            result.Total,
            page,
            pageSize));
    }

    private static ValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
