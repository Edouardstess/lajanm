namespace CavalierNoir.Application.Common.Models;

/// <summary>
/// Clés de signature des liens porteurs d'identité (exercice quotidien,
/// désabonnement, pointage d'événement).
/// </summary>
public sealed class TokenOptions
{
    public const string SectionName = "Tokens";

    /// <summary>
    /// Secret HMAC. Doit être surchargé en production par une variable
    /// d'environnement ou un coffre de secrets ; la valeur par défaut ne
    /// convient qu'au développement local.
    /// </summary>
    public string SigningKey { get; set; } = "cavaliernoir-developpement-clef-a-remplacer";

    /// <summary>Durée de validité d'un lien d'exercice quotidien, en jours.</summary>
    public int DailyExerciseLinkLifetimeDays { get; set; } = 14;
}
