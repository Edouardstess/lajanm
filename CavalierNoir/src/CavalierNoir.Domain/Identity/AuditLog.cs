using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Journal d'audit immuable des actions sensibles (BR-11 : conservation 365 jours).
/// Alimenté automatiquement par l'intercepteur EF Core.
/// </summary>
public class AuditLog : Entity
{
    public int? UserId { get; set; }

    public string? UserName { get; set; }

    /// <summary>« Create », « Update », « Delete », « Login », « Approve »…</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Nom de l'entité concernée, par exemple « Membership ».</summary>
    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? AffectedColumns { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? IpAddress { get; set; }

    public string? CorrelationId { get; set; }
}
