using Lending.Domain;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Lending.Infrastructure.Search;

// FTS configs must match the generated tsvector columns declared in the entity configurations.
// Companies and facilities are queried separately and merged in memory: EF cannot translate a
// UNION over projections that mix converted enum columns with typed null constants.
public sealed class PostgresSearchService(LendingDbContext db) : ISearchService
{
    private const string CompanyFtsConfig = "english";
    private const string FacilityFtsConfig = "simple";

    public async Task<SearchResultSet> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken)
    {
        var take = criteria.Page * criteria.PageSize;
        var total = 0;
        var rows = new List<SearchRow>();

        if (criteria.IncludeCompanies)
        {
            var companies = BuildCompanyQuery(criteria);
            total += await companies.CountAsync(cancellationToken);
            rows.AddRange(await ApplyOrder(companies).Take(take).ToListAsync(cancellationToken));
        }

        if (criteria.IncludeFacilities)
        {
            var facilities = BuildFacilityQuery(criteria);
            total += await facilities.CountAsync(cancellationToken);
            rows.AddRange(await ApplyOrder(facilities).Take(take).ToListAsync(cancellationToken));
        }

        var hits = rows
            .OrderByDescending(r => r.Rank)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(r => new SearchHit(
                r.Type,
                r.Id,
                r.Name,
                r.CompanyName,
                r.CompanyStatus?.ToString() ?? r.FacilityStatus?.ToString() ?? string.Empty,
                r.RepaymentType,
                r.Currency,
                r.Amount,
                r.Outstanding,
                Math.Round(r.Rank, 4)))
            .ToList();

        return new SearchResultSet(hits, total);
    }

    private static IQueryable<SearchRow> ApplyOrder(IQueryable<SearchRow> query) =>
        query.OrderByDescending(r => r.Rank).ThenBy(r => r.Name).ThenBy(r => r.Id);

    private IQueryable<SearchRow> BuildCompanyQuery(SearchCriteria criteria)
    {
        var companies = db.Companies.AsNoTracking();
        if (criteria.CompanyStatus is { } status)
            companies = companies.Where(c => c.Status == status);

        var q = criteria.Query;
        if (q is null)
        {
            return companies.Select(c => new SearchRow
            {
                Type = SearchResultTypes.Company,
                Id = c.Id,
                Name = c.Name,
                CompanyName = c.Name,
                CompanyStatus = c.Status,
                FacilityStatus = null,
                RepaymentType = null,
                Currency = null,
                Amount = null,
                Outstanding = null,
                Rank = 0d
            });
        }

        var pattern = $"%{q}%";
        return companies
            .Where(c =>
                EF.Property<NpgsqlTsVector>(c, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery(CompanyFtsConfig, q))
                || EF.Functions.ILike(c.Name, pattern)
                || EF.Functions.TrigramsAreSimilar(c.Name, q))
            .Select(c => new SearchRow
            {
                Type = SearchResultTypes.Company,
                Id = c.Id,
                Name = c.Name,
                CompanyName = c.Name,
                CompanyStatus = c.Status,
                FacilityStatus = null,
                RepaymentType = null,
                Currency = null,
                Amount = null,
                Outstanding = null,
                Rank = Math.Max(
                    (double)EF.Property<NpgsqlTsVector>(c, "SearchVector")
                        .Rank(EF.Functions.WebSearchToTsQuery(CompanyFtsConfig, q)),
                    EF.Functions.TrigramsSimilarity(c.Name, q))
            });
    }

    private IQueryable<SearchRow> BuildFacilityQuery(SearchCriteria criteria)
    {
        var query = db.Facilities.AsNoTracking()
            .Join(db.Companies.AsNoTracking(), f => f.CompanyId, c => c.Id, (f, c) => new { f, c });

        if (criteria.FacilityStatus is { } status)
            query = query.Where(x => x.f.Status == status);
        if (criteria.RepaymentType is { } repaymentType)
            query = query.Where(x => x.f.RepaymentType == repaymentType);
        if (criteria.Currency is { } currency)
            query = query.Where(x => x.f.Currency == currency);
        if (criteria.MinAmount is { } minAmount)
            query = query.Where(x => x.f.CommitmentAmount >= minAmount);
        if (criteria.MaxAmount is { } maxAmount)
            query = query.Where(x => x.f.CommitmentAmount <= maxAmount);

        var q = criteria.Query;
        if (q is null)
        {
            return query.Select(x => new SearchRow
            {
                Type = SearchResultTypes.Facility,
                Id = x.f.Id,
                Name = x.f.Reference,
                CompanyName = x.c.Name,
                CompanyStatus = null,
                FacilityStatus = x.f.Status,
                RepaymentType = x.f.RepaymentType,
                Currency = x.f.Currency,
                Amount = x.f.CommitmentAmount,
                Outstanding = x.f.OutstandingPrincipal,
                Rank = 0d
            });
        }

        var pattern = $"%{q}%";
        return query
            .Where(x =>
                EF.Property<NpgsqlTsVector>(x.f, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery(FacilityFtsConfig, q))
                || EF.Functions.ILike(x.f.Reference, pattern)
                || EF.Functions.ILike(x.c.Name, pattern)
                || EF.Functions.TrigramsAreSimilar(x.c.Name, q))
            .Select(x => new SearchRow
            {
                Type = SearchResultTypes.Facility,
                Id = x.f.Id,
                Name = x.f.Reference,
                CompanyName = x.c.Name,
                CompanyStatus = null,
                FacilityStatus = x.f.Status,
                RepaymentType = x.f.RepaymentType,
                Currency = x.f.Currency,
                Amount = x.f.CommitmentAmount,
                Outstanding = x.f.OutstandingPrincipal,
                Rank = Math.Max(
                    Math.Max(
                        (double)EF.Property<NpgsqlTsVector>(x.f, "SearchVector")
                            .Rank(EF.Functions.WebSearchToTsQuery(FacilityFtsConfig, q)),
                        EF.Functions.TrigramsSimilarity(x.f.Reference, q)),
                    EF.Functions.TrigramsSimilarity(x.c.Name, q))
            });
    }

    private sealed class SearchRow
    {
        public string Type { get; init; } = string.Empty;
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public CompanyStatus? CompanyStatus { get; init; }
        public FacilityStatus? FacilityStatus { get; init; }
        public RepaymentType? RepaymentType { get; init; }
        public Currency? Currency { get; init; }
        public decimal? Amount { get; init; }
        public decimal? Outstanding { get; init; }
        public double Rank { get; init; }
    }
}
