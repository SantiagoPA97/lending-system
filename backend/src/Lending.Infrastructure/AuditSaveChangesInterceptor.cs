using System.Text.Json;
using System.Text.Json.Serialization;
using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lending.Infrastructure;

public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly HashSet<Type> AuditedTypes =
    [
        typeof(Company),
        typeof(Facility),
        typeof(Repayment),
        typeof(RepaymentScheduleItem)
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
            return;

        context.ChangeTracker.DetectChanges();
        var timestamp = DateTime.UtcNow;
        var user = currentUser.Name ?? currentUser.UserId ?? "system";
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (!AuditedTypes.Contains(entry.Entity.GetType()))
                continue;

            AuditLog? log = entry.State switch
            {
                EntityState.Added => new AuditLog
                {
                    Action = AuditAction.Created,
                    NewValues = Serialize(Snapshot(entry, original: false, onlyModified: false))
                },
                EntityState.Deleted => new AuditLog
                {
                    Action = AuditAction.Deleted,
                    OldValues = Serialize(Snapshot(entry, original: true, onlyModified: false))
                },
                EntityState.Modified => BuildModifiedLog(entry),
                _ => null
            };

            if (log is null)
                continue;

            log.EntityType = entry.Entity.GetType().Name;
            log.EntityId = entry.Property("Id").CurrentValue?.ToString() ?? string.Empty;
            log.UserName = user;
            log.TimestampUtc = timestamp;
            logs.Add(log);
        }

        if (logs.Count > 0)
            context.Set<AuditLog>().AddRange(logs);
    }

    private static AuditLog? BuildModifiedLog(EntityEntry entry)
    {
        var oldValues = Snapshot(entry, original: true, onlyModified: true);
        if (oldValues.Count == 0)
            return null;
        return new AuditLog
        {
            Action = AuditAction.Updated,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(Snapshot(entry, original: false, onlyModified: true))
        };
    }

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool original, bool onlyModified)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsShadowProperty() || property.Metadata.IsConcurrencyToken)
                continue;
            if (onlyModified && !property.IsModified)
                continue;
            values[property.Metadata.Name] = original ? property.OriginalValue : property.CurrentValue;
        }
        return values;
    }

    private static string Serialize(Dictionary<string, object?> values) =>
        JsonSerializer.Serialize(values, JsonOptions);
}
