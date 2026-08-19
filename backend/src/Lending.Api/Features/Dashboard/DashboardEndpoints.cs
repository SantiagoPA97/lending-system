using Lending.Domain;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lending.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(60),
        LocalCacheExpiration = TimeSpan.FromSeconds(60)
    };

    public static RouteGroupBuilder MapDashboardEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/metrics", GetMetrics);
        return group;
    }

    // Plain 60s TTL, deliberately no write invalidation: the dashboard tolerates
    // up-to-a-minute staleness and this keeps every write path cache-unaware.
    private static async Task<IResult> GetMetrics(
        LendingDbContext db,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var metrics = await cache.GetOrCreateAsync(
            "dashboard:metrics",
            async ct => await BuildMetricsAsync(db, ct),
            CacheOptions,
            cancellationToken: cancellationToken);

        return Results.Ok(metrics);
    }

    private static async Task<DashboardMetricsResponse> BuildMetricsAsync(
        LendingDbContext db,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var last30Start = today.AddDays(-30);
        var next30End = today.AddDays(30);

        // Live exposure = Active + Defaulted facilities; amounts are never summed across currencies.
        var portfolio = await db.Facilities.AsNoTracking()
            .Where(f => f.Status == FacilityStatus.Active || f.Status == FacilityStatus.Defaulted)
            .GroupBy(f => f.Currency)
            .Select(g => new CurrencyPortfolioResponse(
                g.Key,
                g.Count(),
                g.Sum(f => f.CommitmentAmount),
                g.Sum(f => f.OutstandingPrincipal)))
            .ToListAsync(ct);

        var statusCounts = await db.Facilities.AsNoTracking()
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var facilitiesByStatus = Enum.GetValues<FacilityStatus>()
            .ToDictionary(s => s, s => statusCounts.FirstOrDefault(c => c.Status == s)?.Count ?? 0);

        // Reversal entries carry negative amounts, so summing yields the net received.
        var repaymentsLast30 = await db.Repayments.AsNoTracking()
            .Where(r => r.PaymentDate >= last30Start && r.PaymentDate <= today)
            .GroupBy(r => r.Currency)
            .Select(g => new CurrencyRepaymentsResponse(
                g.Key,
                g.Count(r => !r.IsReversal),
                g.Sum(r => r.Amount)))
            .ToListAsync(ct);

        var activeInstallments =
            from item in db.ScheduleItems.AsNoTracking()
            join facility in db.Facilities.AsNoTracking() on item.FacilityId equals facility.Id
            where facility.Status == FacilityStatus.Active
            select new { item, facility.Currency };

        var upcoming = await activeInstallments
            .Where(x => x.item.DueDate > today
                        && x.item.DueDate <= next30End
                        && x.item.PrincipalDue - x.item.PrincipalPaid
                           + x.item.InterestDue - x.item.InterestPaid > 0m)
            .GroupBy(x => x.Currency)
            .Select(g => new CurrencyInstallmentsResponse(
                g.Key,
                g.Count(),
                g.Sum(x => x.item.PrincipalDue - x.item.PrincipalPaid
                           + x.item.InterestDue - x.item.InterestPaid)))
            .ToListAsync(ct);

        var overdueQuery = activeInstallments
            .Where(x => x.item.DueDate < today
                        && x.item.PrincipalDue - x.item.PrincipalPaid
                           + x.item.InterestDue - x.item.InterestPaid > 0m);
        var overdueCount = await overdueQuery.CountAsync(ct);
        var oldestOverdue = overdueCount == 0
            ? (DateOnly?)null
            : await overdueQuery.MinAsync(x => x.item.DueDate, ct);

        var exposures = await db.Facilities.AsNoTracking()
            .Where(f => (f.Status == FacilityStatus.Active || f.Status == FacilityStatus.Defaulted)
                        && f.OutstandingPrincipal > 0m)
            .GroupBy(f => new { f.CompanyId, f.Currency })
            .Select(g => new
            {
                g.Key.CompanyId,
                g.Key.Currency,
                Outstanding = g.Sum(f => f.OutstandingPrincipal)
            })
            .ToListAsync(ct);

        var companyIds = exposures.Select(e => e.CompanyId).Distinct().ToList();
        var companyNames = await db.Companies.AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var topExposures = exposures
            .GroupBy(e => e.Currency)
            .OrderBy(g => g.Key)
            .Select(g => new CurrencyExposureResponse(
                g.Key,
                g.OrderByDescending(e => e.Outstanding)
                    .Take(5)
                    .Select(e => new CompanyExposureResponse(
                        e.CompanyId,
                        companyNames.GetValueOrDefault(e.CompanyId, string.Empty),
                        e.Outstanding))
                    .ToList()))
            .ToList();

        return new DashboardMetricsResponse(
            now,
            portfolio.OrderBy(p => p.Currency).ToList(),
            facilitiesByStatus,
            repaymentsLast30.OrderBy(r => r.Currency).ToList(),
            upcoming.OrderBy(u => u.Currency).ToList(),
            new OverdueInstallmentsResponse(overdueCount, oldestOverdue),
            topExposures);
    }
}
