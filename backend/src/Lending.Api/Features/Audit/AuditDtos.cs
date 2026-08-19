using System.Text.Json;

namespace Lending.Api.Features.Audit;

public sealed record AuditLogResponse(
    long Id,
    string EntityType,
    string EntityId,
    string Action,
    string? ChangedBy,
    DateTime TimestampUtc,
    JsonElement? OldValues,
    JsonElement? NewValues);
