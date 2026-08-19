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

    [Fact]
    public async Task ListCompanies_WithInvalidPage_Returns400()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var problem = await (await client.GetAsync("/api/companies?page=0")).ReadProblemAsync(400);
        Assert.True(problem["errors"]!.AsObject().ContainsKey("page"));
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
