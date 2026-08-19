using Lending.Infrastructure.Search;

namespace Lending.Api.Features.Search;

public static class SearchMapping
{
    public static SearchResultResponse ToResponse(this SearchHit hit) =>
        new(
            hit.Type,
            hit.Id,
            hit.Name,
            hit.CompanyName,
            hit.Status,
            hit.RepaymentType,
            hit.Currency,
            hit.Amount,
            hit.Outstanding,
            hit.Rank);
}
