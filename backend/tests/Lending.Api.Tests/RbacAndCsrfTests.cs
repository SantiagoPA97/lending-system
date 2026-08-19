using System.Net;
using System.Net.Http.Json;
using Lending.Api.Common;
using Lending.Api.Features.Companies;
using Lending.Api.Features.Facilities;
using Lending.Api.Features.Repayments;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class RbacAndCsrfTests(PostgresFixture fixture)
{
    [Fact]
    public async Task UnauthenticatedApiRequest_Returns401ProblemJson()
    {
        var client = await fixture.CreateClientAsync();
        var response = await client.GetAsync("/api/companies");
        var problem = await response.ReadProblemAsync(401);
        Assert.Equal("Unauthorized", problem["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task Viewer_CanReadButCannotMutate()
    {
        var client = await fixture.CreateClientAsync("viewer");

        var read = await client.GetAsync("/api/companies");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var mutate = await client.PostAsJsonAsync("/api/companies",
            new CompanyRequest("Viewer Co", "Viewer Co Ltd", "REG-V1", "US", "Retail", "v@example.com"),
            Api.Json);
        await mutate.ReadProblemAsync(403);
    }

    [Fact]
    public async Task Operator_CannotReverseRepayments()
    {
        var client = await fixture.CreateClientAsync("operator");
        var response = await client.PostAsJsonAsync($"/api/repayments/{Guid.NewGuid()}/reverse", new { }, Api.Json);
        await response.ReadProblemAsync(403);
    }

    [Fact]
    public async Task Viewer_CannotCancelFacility()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var response = await client.PostAsJsonAsync($"/api/facilities/{Guid.NewGuid()}/cancel", new { }, Api.Json);
        await response.ReadProblemAsync(403);
    }

    [Fact]
    public async Task Operator_CanReactivateCompany_ButCannotDeactivate()
    {
        var operatorClient = await fixture.CreateClientAsync("operator");
        var adminClient = await fixture.CreateClientAsync("admin");

        var company = await Api.CreateCompanyAsync(operatorClient, "Reactivation Logistics");

        var deniedDeactivate = await operatorClient.PostAsJsonAsync(
            $"/api/companies/{company.Id}/deactivate", new { }, Api.Json);
        await deniedDeactivate.ReadProblemAsync(403);

        var deactivate = await adminClient.PostAsJsonAsync(
            $"/api/companies/{company.Id}/deactivate", new { }, Api.Json);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var reactivate = await operatorClient.PostAsJsonAsync(
            $"/api/companies/{company.Id}/activate", new { }, Api.Json);
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
    }

    [Fact]
    public async Task Admin_ReversingRepayment_RestoresBalances()
    {
        var operatorClient = await fixture.CreateClientAsync("operator");
        var adminClient = await fixture.CreateClientAsync("admin");

        var company = await Api.CreateCompanyAsync(operatorClient, "Reversal Freight");
        var facility = await Api.CreateFacilityAsync(
            operatorClient, company.Id, 100_000m, Currency.USD, 0m, 6, Api.Today, RepaymentType.Bullet);
        await Api.ActivateFacilityAsync(operatorClient, facility.Id);

        var repayment = await (await Api.RepayAsync(operatorClient, facility.Id, 40_000m, Currency.USD, Api.Today))
            .ReadAsAsync<RepaymentResultResponse>();
        Assert.Equal(60_000m, repayment.NewOutstanding);

        var reverse = await adminClient.PostAsJsonAsync(
            $"/api/repayments/{repayment.Repayment.Id}/reverse", new { }, Api.Json);
        Assert.Equal(HttpStatusCode.OK, reverse.StatusCode);
        var reversal = await reverse.ReadAsAsync<RepaymentResultResponse>();

        Assert.True(reversal.Repayment.IsReversal);
        Assert.Equal(-40_000m, reversal.Repayment.Amount);
        Assert.Equal(repayment.Repayment.Id, reversal.Repayment.ReversesRepaymentId);
        Assert.Equal(100_000m, reversal.NewOutstanding);
        Assert.Equal(FacilityStatus.Active, reversal.FacilityStatus);

        var detail = await (await operatorClient.GetAsync($"/api/facilities/{facility.Id}"))
            .ReadAsAsync<FacilityDetailResponse>();
        Assert.Equal(100_000m, detail.OutstandingPrincipal);

        // The ledger is immutable: a repayment can only be reversed once.
        var again = await adminClient.PostAsJsonAsync(
            $"/api/repayments/{repayment.Repayment.Id}/reverse", new { }, Api.Json);
        await again.ReadProblemAsync(422, DomainErrors.Repayment.AlreadyReversed);
    }

    [Fact]
    public async Task Mutation_WithoutCsrfHeader_Returns400()
    {
        var client = await fixture.CreateClientAsync("admin", csrfHeader: false);

        var read = await client.GetAsync("/api/companies");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var mutate = await client.PostAsJsonAsync("/api/companies",
            new CompanyRequest("Csrf Co", "Csrf Co Ltd", "REG-C1", "US", "Retail", "c@example.com"),
            Api.Json);
        await mutate.ReadProblemAsync(400, ApiErrors.Csrf.MissingHeader);
    }
}
