using System.Net;
using System.Net.Http.Json;
using Lending.Api.Features.Facilities;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class DomainRuleTests(PostgresFixture fixture)
{
    [Fact]
    public async Task RecordRepayment_AboveMaxPayable_Returns422()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Overpay Industries");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 50_000m, Currency.USD, 0m, 6, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await Api.RepayAsync(client, facility.Id, 50_000.01m, Currency.USD, Api.Today);
        await response.ReadProblemAsync(422, "repayment.exceeds_outstanding");
    }

    [Fact]
    public async Task CreateFacility_ForInactiveCompany_Returns422()
    {
        var operatorClient = await fixture.CreateClientAsync("operator");
        var adminClient = await fixture.CreateClientAsync("admin");
        var company = await Api.CreateCompanyAsync(operatorClient, "Dormant Holdings");

        var deactivate = await adminClient.PostAsync($"/api/companies/{company.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var response = await operatorClient.PostAsJsonAsync("/api/facilities",
            new CreateFacilityRequest(
                company.Id, 10_000m, Currency.USD, 5m, 12, Api.Today, RepaymentType.Amortizing),
            Api.Json);
        await response.ReadProblemAsync(422, "company.inactive");
    }

    [Fact]
    public async Task UpdateFacility_WhenNotDraft_Returns422()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Locked Terms Corp");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 20_000m, Currency.USD, 5m, 12, Api.Today, RepaymentType.Amortizing);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await client.PutAsJsonAsync($"/api/facilities/{facility.Id}",
            new UpdateFacilityRequest(25_000m, Currency.USD, 5m, 12, Api.Today, RepaymentType.Amortizing),
            Api.Json);
        await response.ReadProblemAsync(422, "facility.not_editable");
    }

    [Fact]
    public async Task ActivateFacility_Twice_Returns422InvalidTransition()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Transition Ltd");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 20_000m, Currency.USD, 5m, 12, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await client.PostAsync($"/api/facilities/{facility.Id}/activate", null);
        await response.ReadProblemAsync(422, "facility.invalid_transition");
    }

    [Fact]
    public async Task RecordRepayment_WithMismatchedCurrency_Returns422()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Currency Strict SA");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 30_000m, Currency.USD, 0m, 6, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await Api.RepayAsync(client, facility.Id, 1_000m, Currency.EUR, Api.Today);
        await response.ReadProblemAsync(422, "repayment.currency_mismatch");
    }
}
