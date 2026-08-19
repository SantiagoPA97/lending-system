using Lending.Domain;

namespace Lending.Api.Features.Search;

public sealed record SearchResultResponse(
    string Type,
    Guid Id,
    string Name,
    string CompanyName,
    string Status,
    RepaymentType? RepaymentType,
    Currency? Currency,
    decimal? Amount,
    decimal? Outstanding,
    double Rank);
