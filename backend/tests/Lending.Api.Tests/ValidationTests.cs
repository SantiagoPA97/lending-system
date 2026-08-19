using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Lending.Api.Features.Companies;
using Lending.Api.Features.Facilities;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class ValidationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task CreateCompany_WithInvalidFields_Returns400WithFieldErrors()
    {
        var client = await fixture.CreateClientAsync("operator");
        var response = await client.PostAsJsonAsync("/api/companies",
            new CompanyRequest("", "Legal", "REG-1", "Colombia", "Freight", "not-an-email"), Api.Json);

        var problem = await response.ReadProblemAsync(400);
        var errors = problem["errors"]!.AsObject();
        Assert.True(errors.ContainsKey("name"));
        Assert.True(errors.ContainsKey("country"));
        Assert.True(errors.ContainsKey("contactEmail"));
        Assert.NotEmpty(errors["name"]!.AsArray());
    }

    [Fact]
    public async Task CreateFacility_WithOutOfRangeTerms_Returns400WithFieldErrors()
    {
        var client = await fixture.CreateClientAsync("operator");
        var response = await client.PostAsJsonAsync("/api/facilities",
            new CreateFacilityRequest(
                Guid.NewGuid(), -5m, Currency.USD, 250m, 0, new DateOnly(2026, 1, 1), RepaymentType.Bullet),
            Api.Json);

        var problem = await response.ReadProblemAsync(400);
        var errors = problem["errors"]!.AsObject();
        Assert.True(errors.ContainsKey("commitmentAmount"));
        Assert.True(errors.ContainsKey("annualInterestRate"));
        Assert.True(errors.ContainsKey("termMonths"));
    }

    [Theory]
    [InlineData("/api/companies")]
    [InlineData("/api/facilities")]
    [InlineData("/api/audit")]
    [InlineData("/api/search")]
    public async Task ListEndpoints_WithOutOfRangePaging_ClampInsteadOf500(string path)
    {
        var client = await fixture.CreateClientAsync("viewer");
        var response = await client.GetAsync($"{path}?page=-3&pageSize=5000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(1, body["page"]!.GetValue<int>());
        Assert.Equal(100, body["pageSize"]!.GetValue<int>());
    }

    [Fact]
    public async Task RecordRepayment_WithFutureDate_Returns400()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Future Payments Inc");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 10_000m, Currency.USD, 5m, 12, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await Api.RepayAsync(client, facility.Id, 100m, Currency.USD, Api.Today.AddDays(1));
        var problem = await response.ReadProblemAsync(400);
        Assert.True(problem["errors"]!.AsObject().ContainsKey("paymentDate"));
    }

    [Fact]
    public async Task RecordRepayment_BeforeFacilityStart_Returns422()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Backdated Payments Inc");
        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 10_000m, Currency.USD, 5m, 12, Api.Today.AddDays(-10), RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(client, facility.Id);

        var response = await Api.RepayAsync(client, facility.Id, 100m, Currency.USD, Api.Today.AddDays(-11));
        await response.ReadProblemAsync(422, DomainErrors.Repayment.BeforeStart);
    }

    [Fact]
    public async Task ListFacilities_WithUnknownStatus_Returns400()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var problem = await (await client.GetAsync("/api/facilities?status=Bogus")).ReadProblemAsync(400);
        var messages = problem["errors"]!["status"]!.AsArray();
        Assert.Contains("Draft", messages[0]!.GetValue<string>());
    }
}
