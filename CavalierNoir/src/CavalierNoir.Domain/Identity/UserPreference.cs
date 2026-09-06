using CavalierNoir.Domain.Common;

namespace CavalierNoir.Domain.Identity;

/// <summary>Préférences d'affichage et de notification, une ligne par utilisateur.</summary>
public class UserPreference : Entity
{
    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    /// <summary>Code de culture : « fr », « ht » (kreyòl) ou « en ».</summary>
    public string Language { get; set; } = "fr";

    /// <summary>« clair », « sombre » ou « systeme ».</summary>
    public string Theme { get; set; } = "systeme";

    public bool ReceiveDailyExercise { get; set; } = true;

    public bool ReceiveNewsletter { get; set; } = true;

    public bool ReceiveTournamentAlerts { get; set; } = true;

    public bool ReceiveForumReplies { get; set; } = true;

    public bool ReceiveMembershipReminders { get; set; } = true;

    /// <summary>Le membre accepte d'apparaître dans les classements publics.</summary>
    public bool ShowInPublicRanking { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
