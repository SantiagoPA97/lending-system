using System.Text.Json;
using Lending.Domain.Entities;

namespace Lending.Api.Features.Audit;

public static class AuditMapping
{
    public static AuditLogResponse ToResponse(this AuditLog log) => new(
        log.Id,
        log.EntityType,
        log.EntityId,
        log.Action,
        log.UserName,
        log.TimestampUtc,
        ParseJson(log.OldValues),
        ParseJson(log.NewValues));

    private static JsonElement? ParseJson(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<JsonElement>(json);
}
