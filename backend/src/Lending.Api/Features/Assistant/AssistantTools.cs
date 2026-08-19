using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic.Models.Messages;
using Lending.Api.Features.Dashboard;
using Lending.Domain;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lending.Api.Features.Assistant;

// Whitelisted read-only tools the model can call. Every tool runs a fixed EF
// query — the model never supplies SQL, only typed filter values.
public static class AssistantTools
{
    private const int MaxResults = 25;

    private static readonly JsonSerializerOptions ResultJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static List<ToolUnion> Definitions() =>
    [
        new Tool
        {
            Name = "search_facilities",
            Description =
                "Search credit facilities in the portfolio. Call this when the question is about specific "
                + "facilities, filtered lists (by status, repayment type, currency, company) or repayment "
                + "progress. Returns at most 25 facilities ordered by outstanding principal.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["query"] = Prop("string", "Free text matched against the facility reference (e.g. FAC-00012) or company name."),
                    ["status"] = EnumProp("Facility status filter.", Enum.GetNames<FacilityStatus>()),
                    ["repaymentType"] = EnumProp("Repayment type filter.", Enum.GetNames<RepaymentType>()),
                    ["currency"] = EnumProp("Currency filter.", Enum.GetNames<Currency>()),
                    ["companyName"] = Prop("string", "Company name filter (partial match)."),
                    ["minOutstanding"] = Prop("number", "Only facilities with outstanding principal at or above this amount."),
                    ["minPercentRepaid"] = Prop("number", "Only activated facilities whose principal is at least this percent repaid (0-100).")
                }
            }
        },
        new Tool
        {
            Name = "get_company",
            Description =
                "Get one company's profile, its facilities and its repayment behaviour (totals per currency, "
                + "last payment date, overdue installments). Identify the company by name or by one of its "
                + "facility references.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["name"] = Prop("string", "Company name (partial match allowed)."),
                    ["reference"] = Prop("string", "A facility reference such as FAC-00012 belonging to the company.")
                }
            }
        },
        new Tool
        {
            Name = "get_portfolio_summary",
            Description =
                "Get the portfolio-wide dashboard metrics: committed and outstanding totals per currency, "
                + "facility counts by status, repayments received in the last 30 days, upcoming and overdue "
                + "installments, and top company exposures per currency. Call this for any portfolio-level or "
                + "exposure question.",
            InputSchema = new() { Properties = new Dictionary<string, JsonElement>() }
        },
        new Tool
        {
            Name = "get_upcoming_payments",
            Description =
                "List scheduled installments due within the next N days on active facilities, with the "
                + "facility and company they belong to. Use for questions about upcoming payments or cash "
                + "expected to come in.",
            InputSchema = new()
            {
                Properties = new Dictionary<string, JsonElement>
                {
                    ["days"] = Prop("integer", "Horizon in days from today (1-365). Defaults to 30.")
                }
            }
        }
    ];

    public static async Task<(string Summary, string ResultJson)> ExecuteAsync(
        string name,
        IReadOnlyDictionary<string, JsonElement> input,
        LendingDbContext db,
        HybridCache cache,
        CancellationToken ct)
    {
        return name switch
        {
            "search_facilities" => await SearchFacilitiesAsync(input, db, ct),
            "get_company" => await GetCompanyAsync(input, db, ct),
            "get_portfolio_summary" => await GetPortfolioSummaryAsync(db, cache, ct),
            "get_upcoming_payments" => await GetUpcomingPaymentsAsync(input, db, ct),
            _ => ($"Unknown tool {name}", Serialize(new { error = $"Unknown tool '{name}'." }))
        };
    }

    private static async Task<(string, string)> SearchFacilitiesAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        LendingDbContext db,
        CancellationToken ct)
    {
        var query = GetString(input, "query");
        var companyName = GetString(input, "companyName");
        var minOutstanding = GetDecimal(input, "minOutstanding");
        var minPercentRepaid = GetDecimal(input, "minPercentRepaid");
        var status = GetEnum<FacilityStatus>(input, "status");
        var repaymentType = GetEnum<RepaymentType>(input, "repaymentType");
        var currency = GetEnum<Currency>(input, "currency");

        var facilities =
            from f in db.Facilities.AsNoTracking()
            join c in db.Companies.AsNoTracking() on f.CompanyId equals c.Id
            select new { f, c };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            facilities = facilities.Where(x =>
                EF.Functions.ILike(x.f.Reference, pattern) || EF.Functions.ILike(x.c.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var pattern = $"%{companyName.Trim()}%";
            facilities = facilities.Where(x => EF.Functions.ILike(x.c.Name, pattern));
        }

        if (status is not null)
            facilities = facilities.Where(x => x.f.Status == status);
        if (repaymentType is not null)
            facilities = facilities.Where(x => x.f.RepaymentType == repaymentType);
        if (currency is not null)
            facilities = facilities.Where(x => x.f.Currency == currency);
        if (minOutstanding is not null)
            facilities = facilities.Where(x => x.f.OutstandingPrincipal >= minOutstanding);

        if (minPercentRepaid is not null)
        {
            // percentRepaid >= min  <=>  outstanding <= (1 - min/100) * commitment,
            // which translates to SQL so the filter runs before the row cap.
            var remainingFraction = 1m - Math.Clamp(minPercentRepaid.Value, 0m, 100m) / 100m;
            facilities = facilities.Where(x =>
                x.f.Status != FacilityStatus.Draft
                && x.f.Status != FacilityStatus.Cancelled
                && x.f.OutstandingPrincipal <= x.f.CommitmentAmount * remainingFraction);
        }

        var rows = await facilities
            .OrderByDescending(x => x.f.OutstandingPrincipal)
            .ThenBy(x => x.f.Reference)
            .Select(x => new
            {
                x.f.Reference,
                Company = x.c.Name,
                x.f.Status,
                x.f.RepaymentType,
                x.f.Currency,
                x.f.CommitmentAmount,
                x.f.OutstandingPrincipal,
                x.f.AnnualInterestRate,
                x.f.TermMonths,
                x.f.StartDate
            })
            .Take(MaxResults)
            .ToListAsync(ct);

        var results = rows
            .Select(x => new
            {
                x.Reference,
                x.Company,
                x.Status,
                x.RepaymentType,
                x.Currency,
                Commitment = x.CommitmentAmount,
                Outstanding = x.OutstandingPrincipal,
                AnnualInterestRatePercent = x.AnnualInterestRate,
                x.TermMonths,
                x.StartDate,
                // Percent of principal repaid — only meaningful once the facility was activated.
                PercentRepaid = x.Status is FacilityStatus.Draft or FacilityStatus.Cancelled
                    ? (decimal?)null
                    : Math.Round((x.CommitmentAmount - x.OutstandingPrincipal) / x.CommitmentAmount * 100m, 1)
            })
            .ToList();

        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) filters.Add($"\"{query.Trim()}\"");
        if (!string.IsNullOrWhiteSpace(companyName)) filters.Add($"company~{companyName.Trim()}");
        if (status is not null) filters.Add($"status={status}");
        if (repaymentType is not null) filters.Add($"type={repaymentType}");
        if (currency is not null) filters.Add($"currency={currency}");
        if (minOutstanding is not null) filters.Add($"outstanding>={minOutstanding}");
        if (minPercentRepaid is not null) filters.Add($"repaid>={minPercentRepaid}%");
        var summary = filters.Count == 0
            ? $"Searched facilities ({results.Count} result{(results.Count == 1 ? "" : "s")})"
            : $"Searched facilities [{string.Join(", ", filters)}] ({results.Count} result{(results.Count == 1 ? "" : "s")})";

        return (summary, Serialize(new { count = results.Count, truncatedAt = MaxResults, facilities = results }));
    }

    private static async Task<(string, string)> GetCompanyAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        LendingDbContext db,
        CancellationToken ct)
    {
        var name = GetString(input, "name");
        var reference = GetString(input, "reference");

        Guid? companyId = null;
        if (!string.IsNullOrWhiteSpace(reference))
        {
            companyId = await db.Facilities.AsNoTracking()
                .Where(f => EF.Functions.ILike(f.Reference, reference.Trim()))
                .Select(f => (Guid?)f.CompanyId)
                .FirstOrDefaultAsync(ct);
        }

        if (companyId is null && !string.IsNullOrWhiteSpace(name))
        {
            var pattern = $"%{name.Trim()}%";
            companyId = await db.Companies.AsNoTracking()
                .Where(c => EF.Functions.ILike(c.Name, pattern))
                .OrderBy(c => c.Name)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);
        }

        var label = name ?? reference ?? "?";
        if (companyId is null)
            return ($"Looked up company \"{label}\" (not found)",
                Serialize(new { found = false, message = "No company matched the given name or facility reference." }));

        var company = await db.Companies.AsNoTracking().FirstAsync(c => c.Id == companyId, ct);
        var facilities = await db.Facilities.AsNoTracking()
            .Where(f => f.CompanyId == companyId)
            .OrderBy(f => f.Reference)
            .ToListAsync(ct);

        var facilityIds = facilities.Select(f => f.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var repaymentTotals = await db.Repayments.AsNoTracking()
            .Where(r => facilityIds.Contains(r.FacilityId))
            .GroupBy(r => r.Currency)
            .Select(g => new
            {
                Currency = g.Key,
                Payments = g.Count(r => !r.IsReversal),
                Reversals = g.Count(r => r.IsReversal),
                // Reversals carry negative amounts, so sums are net figures.
                NetPrincipalRepaid = g.Sum(r => r.PrincipalApplied),
                NetInterestPaid = g.Sum(r => r.InterestApplied),
                LastPaymentDate = g.Where(r => !r.IsReversal).Max(r => (DateOnly?)r.PaymentDate)
            })
            .ToListAsync(ct);

        var overdue = await db.ScheduleItems.AsNoTracking()
            .Join(db.Facilities.AsNoTracking().Where(f => f.CompanyId == companyId && f.Status == FacilityStatus.Active),
                i => i.FacilityId, f => f.Id, (i, f) => i)
            .Where(i => i.DueDate < today
                        && i.PrincipalDue - i.PrincipalPaid + i.InterestDue - i.InterestPaid > 0m)
            .CountAsync(ct);

        var result = new
        {
            found = true,
            company = new
            {
                company.Name,
                company.LegalName,
                company.RegistrationNumber,
                company.Country,
                company.Industry,
                company.Status
            },
            facilities = facilities.Select(f => new
            {
                f.Reference,
                f.Status,
                f.RepaymentType,
                f.Currency,
                Commitment = f.CommitmentAmount,
                Outstanding = f.OutstandingPrincipal,
                AnnualInterestRatePercent = f.AnnualInterestRate,
                f.TermMonths,
                f.StartDate
            }),
            repaymentBehaviour = new
            {
                perCurrency = repaymentTotals,
                overdueInstallments = overdue
            }
        };

        return ($"Looked up company \"{company.Name}\"", Serialize(result));
    }

    private static async Task<(string, string)> GetPortfolioSummaryAsync(
        LendingDbContext db,
        HybridCache cache,
        CancellationToken ct)
    {
        var metrics = await DashboardEndpoints.GetCachedMetricsAsync(db, cache, ct);
        return ("Fetched portfolio summary", Serialize(metrics));
    }

    private static async Task<(string, string)> GetUpcomingPaymentsAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        LendingDbContext db,
        CancellationToken ct)
    {
        var days = (int)Math.Clamp(GetDecimal(input, "days") ?? 30m, 1m, 365m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(days);

        var items = await (
                from i in db.ScheduleItems.AsNoTracking()
                join f in db.Facilities.AsNoTracking() on i.FacilityId equals f.Id
                join c in db.Companies.AsNoTracking() on f.CompanyId equals c.Id
                where f.Status == FacilityStatus.Active
                      && i.DueDate > today
                      && i.DueDate <= horizon
                      && i.PrincipalDue - i.PrincipalPaid + i.InterestDue - i.InterestPaid > 0m
                orderby i.DueDate, f.Reference
                select new
                {
                    i.DueDate,
                    i.Period,
                    FacilityReference = f.Reference,
                    Company = c.Name,
                    f.Currency,
                    PrincipalDue = i.PrincipalDue - i.PrincipalPaid,
                    InterestDue = i.InterestDue - i.InterestPaid,
                    TotalDue = i.PrincipalDue - i.PrincipalPaid + i.InterestDue - i.InterestPaid
                })
            .Take(50)
            .ToListAsync(ct);

        return ($"Fetched payments due in the next {days} days ({items.Count} installment{(items.Count == 1 ? "" : "s")})",
            Serialize(new { horizonDays = days, count = items.Count, truncatedAt = 50, installments = items }));
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, ResultJson);

    private static JsonElement Prop(string type, string description) =>
        JsonSerializer.SerializeToElement(new { type, description });

    private static JsonElement EnumProp(string description, string[] values) =>
        JsonSerializer.SerializeToElement(new { type = "string", description, @enum = values });

    private static string? GetString(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private static decimal? GetDecimal(IReadOnlyDictionary<string, JsonElement> input, string key)
    {
        if (!input.TryGetValue(key, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.Number => el.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(el.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static TEnum? GetEnum<TEnum>(IReadOnlyDictionary<string, JsonElement> input, string key)
        where TEnum : struct, Enum
    {
        var raw = GetString(input, key);
        return raw is not null && Enum.TryParse<TEnum>(raw.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
