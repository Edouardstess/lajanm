using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Trace de connexion (réussie ou non). Alimente la détection d'anomalies et la
/// non-répudiation (STRIDE – Repudiation).
/// </summary>
public class LoginHistory : Entity
{
    public int? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    /// <summary>Identifiant saisi, conservé même quand aucun compte ne correspond.</summary>
    public string AttemptedIdentifier { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }
}
