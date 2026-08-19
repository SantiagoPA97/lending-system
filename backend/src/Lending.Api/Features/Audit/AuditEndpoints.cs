using FluentValidation;
using FluentValidation.Results;
using Lending.Api.Common;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Features.Audit;

public static class AuditEndpoints
{
    public static RouteGroupBuilder MapAuditEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetList);
        return group;
    }

    private static async Task<IResult> GetList(
        LendingDbContext db,
        CancellationToken cancellationToken,
        string? entityType = null,
        string? entityId = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1)
            throw Invalid("page", "page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw Invalid("pageSize", "pageSize must be between 1 and 100.");

        var query = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var typeFilter = entityType.Trim();
            query = query.Where(a => EF.Functions.ILike(a.EntityType, typeFilter));
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            var idFilter = entityId.Trim();
            query = query.Where(a => a.EntityId == idFilter);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.TimestampUtc)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<AuditLogResponse>(
            items.Select(a => a.ToResponse()).ToList(),
            total,
            page,
            pageSize));
    }

    private static ValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
