using Lending.Api.Features.Dashboard;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class DashboardTests(PostgresFixture fixture)
{
    // GBP and COP are reserved for this test class so the per-currency totals can be
    // asserted exactly against a database shared with the other test classes.
    [Fact]
    public async Task Metrics_KeepCurrenciesSeparate_NeverSummingAcrossThem()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Dashboard Treasury");

        var gbp = await Api.CreateFacilityAsync(
            client, company.Id, 5_000_000m, Currency.GBP, 0m, 12, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, gbp.Id);
        var cop = await Api.CreateFacilityAsync(
            client, company.Id, 9_000_000m, Currency.COP, 0m, 12, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, cop.Id);

        var repay = await Api.RepayAsync(client, gbp.Id, 1_000_000m, Currency.GBP, Api.Today);
        Assert.True(repay.IsSuccessStatusCode);

        // A fresh factory shares the database but not the 60s HybridCache entry,
        // so the metrics snapshot is guaranteed to include the data created above.
        await using var freshFactory = fixture.CreateFactory();
        var viewer = await fixture.CreateClientAsync("viewer", factory: freshFactory);
        var metrics = await (await viewer.GetAsync("/api/dashboard/metrics"))
            .ReadAsAsync<DashboardMetricsResponse>();

        Assert.Equal(
            metrics.Portfolio.Count,
            metrics.Portfolio.Select(p => p.Currency).Distinct().Count());

        var gbpPortfolio = Assert.Single(metrics.Portfolio, p => p.Currency == Currency.GBP);
        Assert.Equal(1, gbpPortfolio.FacilityCount);
        Assert.Equal(5_000_000m, gbpPortfolio.TotalCommitted);
        Assert.Equal(4_000_000m, gbpPortfolio.TotalOutstanding);

        var copPortfolio = Assert.Single(metrics.Portfolio, p => p.Currency == Currency.COP);
        Assert.Equal(1, copPortfolio.FacilityCount);
        Assert.Equal(9_000_000m, copPortfolio.TotalCommitted);
        Assert.Equal(9_000_000m, copPortfolio.TotalOutstanding);

        var gbpRepayments = Assert.Single(metrics.RepaymentsLast30Days, r => r.Currency == Currency.GBP);
        Assert.Equal(1, gbpRepayments.Count);
        Assert.Equal(1_000_000m, gbpRepayments.NetAmount);
        Assert.DoesNotContain(metrics.RepaymentsLast30Days, r => r.Currency == Currency.COP);

        Assert.True(metrics.FacilitiesByStatus[FacilityStatus.Active] >= 2);
        Assert.All(metrics.TopCompanyExposures, e =>
            Assert.True(e.Companies.Count is > 0 and <= 5));
        var gbpExposure = Assert.Single(metrics.TopCompanyExposures, e => e.Currency == Currency.GBP);
        Assert.Contains(gbpExposure.Companies, c => c.CompanyId == company.Id && c.Outstanding == 4_000_000m);
    }
}
