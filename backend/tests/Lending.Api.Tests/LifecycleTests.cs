using System.Net;
using System.Net.Http.Json;
using Lending.Api.Common;
using Lending.Api.Features.Audit;
using Lending.Api.Features.Facilities;
using Lending.Api.Features.Repayments;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class LifecycleTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SchedulePreview_Bullet_ComputesSimpleInterest()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var response = await client.PostAsJsonAsync("/api/facilities/schedule-preview",
            new SchedulePreviewRequest(100_000m, Currency.USD, 12m, 6, new DateOnly(2026, 1, 15), RepaymentType.Bullet),
            Api.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.ReadAsAsync<SchedulePreviewResponse>();

        var installment = Assert.Single(preview.Installments);
        // 100,000 * 1%/month * 6 months (30/360 simple interest)
        Assert.Equal(6_000m, installment.InterestDue);
        Assert.Equal(100_000m, installment.PrincipalDue);
        Assert.Equal(new DateOnly(2026, 7, 15), installment.DueDate);
        Assert.Equal(0m, installment.RemainingBalance);
        Assert.Equal(106_000m, preview.TotalPayable);
    }

    [Fact]
    public async Task SchedulePreview_AmortizingZeroRate_SplitsPrincipalEvenly()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var response = await client.PostAsJsonAsync("/api/facilities/schedule-preview",
            new SchedulePreviewRequest(12_000m, Currency.EUR, 0m, 12, new DateOnly(2026, 3, 1), RepaymentType.Amortizing),
            Api.Json);

        var preview = await response.ReadAsAsync<SchedulePreviewResponse>();
        Assert.Equal(12, preview.Installments.Count);
        Assert.All(preview.Installments, i => Assert.Equal(1_000m, i.PrincipalDue));
        Assert.All(preview.Installments, i => Assert.Equal(0m, i.InterestDue));
        Assert.Equal(12_000m, preview.TotalPrincipal);
        Assert.Equal(0m, preview.TotalInterest);
    }

    [Fact]
    public async Task SchedulePreview_Amortizing_PrincipalSumsExactlyToCommitment()
    {
        var client = await fixture.CreateClientAsync("viewer");
        var response = await client.PostAsJsonAsync("/api/facilities/schedule-preview",
            new SchedulePreviewRequest(100_000m, Currency.USD, 7.35m, 12, new DateOnly(2026, 2, 28), RepaymentType.Amortizing),
            Api.Json);

        var preview = await response.ReadAsAsync<SchedulePreviewResponse>();
        Assert.Equal(12, preview.Installments.Count);
        Assert.Equal(100_000m, preview.Installments.Sum(i => i.PrincipalDue));
        Assert.Equal(0m, preview.Installments[^1].RemainingBalance);
        Assert.All(preview.Installments, i => Assert.Equal(decimal.Round(i.PrincipalDue, 2), i.PrincipalDue));
    }

    [Fact]
    public async Task FullLifecycle_CreateActivateRepayToZero_CompletesWithAuditTrail()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Lifecycle Manufacturing");
        var start = Api.Today.AddMonths(-4); // all three periods already due

        var facility = await Api.CreateFacilityAsync(
            client, company.Id, 100_000m, Currency.USD, 12m, 3, start, RepaymentType.InterestOnly);
        Assert.Equal(FacilityStatus.Draft, facility.Status);
        Assert.StartsWith("FAC-", facility.Reference);
        Assert.Equal(0m, facility.OutstandingPrincipal);

        var activated = await Api.ActivateFacilityAsync(client, facility.Id);
        Assert.Equal(FacilityStatus.Active, activated.Status);
        Assert.Equal(100_000m, activated.OutstandingPrincipal);

        var schedule = await (await client.GetAsync($"/api/facilities/{facility.Id}/schedule"))
            .ReadAsAsync<ScheduleResponse>();
        Assert.Equal(3, schedule.Items.Count);
        Assert.All(schedule.Items, i => Assert.Equal(1_000m, i.InterestDue)); // 100k * 12%/12
        Assert.Equal(start.AddMonths(1), schedule.Items[0].DueDate);
        Assert.Equal(100_000m, schedule.TotalPrincipal);
        Assert.Equal(3_000m, schedule.TotalInterest);
        Assert.Equal(103_000m, schedule.TotalPayable);

        // Partial payment allocates interest-first against the oldest unpaid periods.
        var partialResponse = await Api.RepayAsync(client, facility.Id, 1_500m, Currency.USD, Api.Today);
        Assert.Equal(HttpStatusCode.Created, partialResponse.StatusCode);
        var partial = await partialResponse.ReadAsAsync<RepaymentResultResponse>();
        Assert.Equal(1_500m, partial.InterestPaid);
        Assert.Equal(0m, partial.PrincipalPaid);
        Assert.Equal(100_000m, partial.NewOutstanding);
        Assert.Equal(FacilityStatus.Active, partial.FacilityStatus);
        Assert.Equal(
            [(1, 1_000m), (2, 500m)],
            partial.Repayment.Allocations.Select(a => (a.Period, a.Interest)).ToArray());

        // Remaining 1,500 interest + full principal completes the facility.
        var finalResponse = await Api.RepayAsync(client, facility.Id, 101_500m, Currency.USD, Api.Today);
        Assert.Equal(HttpStatusCode.Created, finalResponse.StatusCode);
        var final = await finalResponse.ReadAsAsync<RepaymentResultResponse>();
        Assert.Equal(1_500m, final.InterestPaid);
        Assert.Equal(100_000m, final.PrincipalPaid);
        Assert.Equal(0m, final.NewOutstanding);
        Assert.Equal(FacilityStatus.Completed, final.FacilityStatus);

        var detail = await (await client.GetAsync($"/api/facilities/{facility.Id}"))
            .ReadAsAsync<FacilityDetailResponse>();
        Assert.Equal(FacilityStatus.Completed, detail.Status);
        Assert.Equal(0m, detail.OutstandingPrincipal);
        Assert.Equal(100_000m, detail.TotalPrincipalPaid);
        Assert.Equal(3_000m, detail.TotalInterestPaid);

        var repayments = await (await client.GetAsync($"/api/facilities/{facility.Id}/repayments"))
            .ReadAsAsync<List<RepaymentResponse>>();
        Assert.Equal(2, repayments.Count);

        // Completed facilities accept no further repayments.
        var rejected = await Api.RepayAsync(client, facility.Id, 100m, Currency.USD, Api.Today);
        await rejected.ReadProblemAsync(422, DomainErrors.Facility.NotActive);

        // Audit trail exists and carries the dev-login user attribution.
        var facilityAudit = await (await client.GetAsync(
                $"/api/audit?entityType=Facility&entityId={facility.Id}&pageSize=50"))
            .ReadAsAsync<PagedResult<AuditLogResponse>>();
        Assert.True(facilityAudit.Total >= 3, $"Expected >=3 facility audit rows, got {facilityAudit.Total}");
        Assert.Contains(facilityAudit.Items, a => a.Action == "Created");
        Assert.Contains(facilityAudit.Items, a => a.Action == "Updated");
        Assert.All(facilityAudit.Items, a => Assert.Equal("Demo Operator", a.ChangedBy));

        var repaymentAudit = await (await client.GetAsync(
                $"/api/audit?entityType=Repayment&entityId={partial.Repayment.Id}"))
            .ReadAsAsync<PagedResult<AuditLogResponse>>();
        var entry = Assert.Single(repaymentAudit.Items);
        Assert.Equal("Created", entry.Action);
        Assert.Equal("Demo Operator", entry.ChangedBy);
    }
}
