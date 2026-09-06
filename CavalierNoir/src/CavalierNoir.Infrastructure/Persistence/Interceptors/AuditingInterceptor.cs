using System.Text.Json;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CavalierNoir.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Renseigne automatiquement les colonnes d'audit (<c>CreatedAt</c>,
/// <c>CreatedById</c>, <c>UpdatedAt</c>, <c>UpdatedById</c>) et journalise les
/// modifications des entités sensibles dans <c>JournalAudit</c> (BR-11).
/// </summary>
public sealed class AuditingInterceptor(ICurrentUser currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    /// <summary>
    /// Entités dont chaque écriture est tracée. La liste reste courte
    /// volontairement : journaliser tout produirait un volume ingérable sans
    /// valeur probante supplémentaire.
    /// </summary>
    private static readonly HashSet<string> AuditedEntities =
    [
        "Membership",
        "MembershipApplication",
        "Payment",
        "Donation",
        "Expense",
        "Document",
        "Tournament",
        "TournamentGame",
        "ApplicationUser",
        "SystemSetting",
        "Meeting"
    ];

    private static readonly HashSet<string> RedactedProperties =
    [
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "ConfirmationToken",
        "UnsubscribeToken",
        "CheckInToken",
        "TransactionId"
    ];

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var userId = currentUser.UserId;
        var auditRows = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog or LoginHistory)
            {
                continue;
            }

            if (entry.Entity is AuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = auditable.CreatedAt == default ? now : auditable.CreatedAt;
                    auditable.CreatedById ??= userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedById ??= userId;
                }
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;
            if (!AuditedEntities.Contains(entityName))
            {
                continue;
            }

            auditRows.Add(BuildAuditRow(entry, entityName, userId, now));
        }

        if (auditRows.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditRows);
        }
    }

    private AuditLog BuildAuditRow(EntityEntry entry, string entityName, int? userId, DateTime now)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var changed = new List<string>();

        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (RedactedProperties.Contains(name))
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[name] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    oldValues[name] = property.OriginalValue;
                    break;

                case EntityState.Modified when property.IsModified:
                    oldValues[name] = property.OriginalValue;
                    newValues[name] = property.CurrentValue;
                    changed.Add(name);
                    break;
            }
        }

        var action = entry.State switch
        {
            EntityState.Added => "Create",
            EntityState.Deleted => "Delete",
            _ => "Update"
        };

        var key = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString();

        return new AuditLog
        {
            UserId = userId,
            UserName = currentUser.UserName,
            Action = action,
            EntityName = entityName,
            EntityId = key,
            OldValues = oldValues.Count == 0 ? null : Serialize(oldValues),
            NewValues = newValues.Count == 0 ? null : Serialize(newValues),
            AffectedColumns = changed.Count == 0 ? null : string.Join(',', changed),
            Timestamp = now,
            IpAddress = currentUser.IpAddress,
            CorrelationId = currentUser.CorrelationId
        };
    }

    private static string Serialize(Dictionary<string, object?> values)
    {
        try
        {
            return JsonSerializer.Serialize(values, JsonOptions);
        }
        catch (NotSupportedException)
        {
            // Une valeur non sérialisable ne doit jamais faire échouer l'enregistrement.
            return "{}";
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
