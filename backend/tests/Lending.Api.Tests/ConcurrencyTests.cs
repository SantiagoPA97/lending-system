using System.Net;
using Lending.Api.Features.Facilities;
using Lending.Domain;

namespace Lending.Api.Tests;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests(PostgresFixture fixture)
{
    // Best-effort race: both requests try to repay the full outstanding at once.
    // When the writes truly overlap, xmin optimistic concurrency turns the loser
    // into a 409. When the requests happen to serialize, the loser instead sees a
    // Completed facility (422 facility.not_active) — that outcome is retried a few
    // times rather than failed, because forcing the interleaving from the outside
    // is inherently timing-dependent. Balance integrity is asserted on every attempt.
    [Fact]
    public async Task ConcurrentFullRepayments_ExactlyOneSucceeds()
    {
        var client = await fixture.CreateClientAsync("operator");
        var company = await Api.CreateCompanyAsync(client, "Concurrent Ventures");
        var observed409 = false;

        for (var attempt = 0; attempt < 5 && !observed409; attempt++)
        {
            var facility = await Api.CreateFacilityAsync(
                client, company.Id, 100_000m, Currency.USD, 0m, 6, Api.Today, RepaymentType.Bullet);
            await Api.ActivateFacilityAsync(client, facility.Id);

            var responses = await Task.WhenAll(
                Api.RepayAsync(client, facility.Id, 100_000m, Currency.USD, Api.Today),
                Api.RepayAsync(client, facility.Id, 100_000m, Currency.USD, Api.Today));

            var statuses = responses.Select(r => r.StatusCode).ToArray();
            Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));

            var loser = statuses.Single(s => s != HttpStatusCode.Created);
            Assert.True(loser is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity,
                $"Expected the losing request to get 409 or 422, got {(int)loser}");
            observed409 = loser == HttpStatusCode.Conflict;

            // Regardless of which failure shape the loser got, exactly one payment landed.
            var detail = await (await client.GetAsync($"/api/facilities/{facility.Id}"))
                .ReadAsAsync<FacilityDetailResponse>();
            Assert.Equal(0m, detail.OutstandingPrincipal);
            Assert.Equal(100_000m, detail.TotalPrincipalPaid);
            Assert.Equal(FacilityStatus.Completed, detail.Status);

            var repayments = await (await client.GetAsync($"/api/facilities/{facility.Id}/repayments"))
                .ReadAsAsync<List<Lending.Api.Features.Repayments.RepaymentResponse>>();
            Assert.Single(repayments);
        }
    }
}
