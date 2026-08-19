using Lending.Api.Common;
using Lending.Api.Features.Search;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class SearchTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Search_Companies_FullTextFuzzyAndStatusFilter()
    {
        var operatorClient = await fixture.CreateClientAsync("operator");
        var adminClient = await fixture.CreateClientAsync("admin");
        var northwind = await Api.CreateCompanyAsync(operatorClient, "Northwind Logistics");
        var aurora = await Api.CreateCompanyAsync(operatorClient, "Aurora Metalworks");
        await adminClient.PostAsync($"/api/companies/{aurora.Id}/deactivate", null);

        // FTS: single-token web search over the generated tsvector.
        var fts = await Search(operatorClient, "q=logistics&type=company");
        Assert.Contains(fts.Items, h => h.Id == northwind.Id && h.Type == "company");
        Assert.DoesNotContain(fts.Items, h => h.Id == aurora.Id);

        // Trigram fuzzy: transposed letters still resolve to the company.
        var fuzzy = await Search(operatorClient, "q=Northwind%20Logistcs&type=company");
        Assert.Contains(fuzzy.Items, h => h.Id == northwind.Id);
        Assert.True(fuzzy.Items.First(h => h.Id == northwind.Id).Rank > 0);

        // Status filter narrows to inactive companies only.
        var inactive = await Search(operatorClient, "type=company&status=Inactive&pageSize=100");
        Assert.Contains(inactive.Items, h => h.Id == aurora.Id);
        Assert.DoesNotContain(inactive.Items, h => h.Id == northwind.Id);
        Assert.All(inactive.Items, h => Assert.Equal("Inactive", h.Status));
    }

    [Fact]
    public async Task Search_Facilities_FiltersAndReferenceLookup()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Searchable Freight");
        var bulletDraft = await Api.CreateFacilityAsync(
            client, company.Id, 50_000m, Currency.USD, 6m, 12, Api.Today, RepaymentType.Bullet);
        var amortizing = await Api.CreateFacilityAsync(
            client, company.Id, 75_000m, Currency.EUR, 6m, 12, Api.Today, RepaymentType.Amortizing);
        await Api.ActivateFacilityAsync(client, amortizing.Id);

        // Facility-only filters exclude company hits entirely.
        var byCurrency = await Search(client, "currency=EUR&pageSize=100");
        Assert.All(byCurrency.Items, h => Assert.Equal("facility", h.Type));
        Assert.Contains(byCurrency.Items, h => h.Id == amortizing.Id);
        Assert.DoesNotContain(byCurrency.Items, h => h.Id == bulletDraft.Id);

        // Amount range: only the 75k facility falls inside 60k–80k.
        var byAmount = await Search(client, "minAmount=60000&maxAmount=80000&pageSize=100");
        var amountHit = Assert.Single(byAmount.Items);
        Assert.Equal(amortizing.Reference, amountHit.Name);
        Assert.Equal(75_000m, amountHit.Amount);

        // Exact reference lookup ranks the facility as a match.
        var byReference = await Search(client, $"q={bulletDraft.Reference}&type=facility");
        Assert.Contains(byReference.Items, h => h.Id == bulletDraft.Id && h.Rank > 0);

        // Combining query + repaymentType + status narrows to the draft bullet.
        var combined = await Search(client,
            "q=Searchable%20Freight&type=facility&repaymentType=Bullet&status=Draft&pageSize=100");
        Assert.Contains(combined.Items, h => h.Id == bulletDraft.Id);
        Assert.DoesNotContain(combined.Items, h => h.Id == amortizing.Id);
        Assert.All(combined.Items, h => Assert.Equal("Searchable Freight", h.CompanyName));
    }

    [Fact]
    public async Task Search_Paging_ReportsTotalAcrossPages()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Paging Partners");
        for (var i = 0; i < 3; i++)
            await Api.CreateFacilityAsync(
                client, company.Id, 11_111m, Currency.USD, 3m, 6, Api.Today, RepaymentType.Bullet);

        var page1 = await Search(client, "q=Paging%20Partners&type=facility&page=1&pageSize=2");
        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);

        var page2 = await Search(client, "q=Paging%20Partners&type=facility&page=2&pageSize=2");
        Assert.Equal(3, page2.Total);
        Assert.Single(page2.Items);
        Assert.Empty(page1.Items.Select(h => h.Id).Intersect(page2.Items.Select(h => h.Id)));
    }

    private static async Task<PagedResult<SearchResultResponse>> Search(HttpClient client, string queryString) =>
        await (await client.GetAsync($"/api/search?{queryString}"))
            .ReadAsAsync<PagedResult<SearchResultResponse>>();
}
