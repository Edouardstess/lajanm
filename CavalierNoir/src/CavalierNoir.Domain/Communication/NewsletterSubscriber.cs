using CavalierNoir.Domain.Common;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Domain.Communication;

/// <summary>
/// Abonné à la lettre d'information. Un abonné n'est pas forcément un membre :
/// le consentement est explicite et horodaté (RGPD).
/// </summary>
public class NewsletterSubscriber : Entity
{
    public string Email { get; set; } = string.Empty;

    public string? Name { get; set; }

    public int? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Abonnement confirmé par clic sur le lien de validation (double opt-in).</summary>
    public bool IsConfirmed { get; set; }

    public string? ConfirmationToken { get; set; }

    /// <summary>Jeton permanent servant au lien de désabonnement en un clic.</summary>
    public string UnsubscribeToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? UnsubscribedAt { get; set; }

    public string? SubscriptionIp { get; set; }

    /// <summary>Souhaite aussi recevoir l'exercice quotidien.</summary>
    public bool WantsDailyExercise { get; set; } = true;

    public void Confirm(DateTime when)
    {
        IsConfirmed = true;
        ConfirmedAt = when;
        ConfirmationToken = null;
        IsActive = true;
        UnsubscribedAt = null;
    }

    public void Unsubscribe(DateTime when)
    {
        IsActive = false;
        UnsubscribedAt = when;
    }
}
