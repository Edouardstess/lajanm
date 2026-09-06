using System.Security.Cryptography;
using System.Text;
using CavalierNoir.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Jetons signés des liens « résoudre l'exercice du jour » envoyés par courriel.
/// Le jeton porte l'identifiant du membre et la date : il est vérifiable sans
/// stockage et ne peut pas être forgé (HMAC-SHA256).
/// </summary>
public sealed class ExerciseTokenService(IOptions<TokenOptions> options)
{
    private readonly TokenOptions _options = options.Value;

    /// <summary>
    /// Construit le jeton « identifiant.date.signature ». L'identifiant 0 désigne
    /// un destinataire anonyme (abonné à la lettre d'information sans compte) :
    /// le lien ouvre l'exercice sans enregistrer de tentative.
    /// </summary>
    public string Create(int userId, DateOnly date)
    {
        var payload = $"{userId}.{date:yyyyMMdd}";
        return $"{payload}.{Sign(payload)}";
    }

    /// <summary>
    /// Vérifie un jeton et en extrait le membre et la date. Rejette une
    /// signature invalide ou un lien périmé.
    /// </summary>
    public bool TryValidate(string? token, out int userId, out DateOnly date)
    {
        userId = 0;
        date = default;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var parsedUserId) || parsedUserId < 0)
        {
            return false;
        }

        if (!DateOnly.TryParseExact(
                parts[1],
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDate))
        {
            return false;
        }

        var expected = Sign($"{parts[0]}.{parts[1]}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[2])))
        {
            return false;
        }

        userId = parsedUserId;
        date = parsedDate;
        return true;
    }

    /// <summary>Le lien est-il encore dans sa fenêtre de validité ?</summary>
    public bool IsExpired(DateOnly linkDate, DateOnly today) =>
        linkDate.AddDays(_options.DailyExerciseLinkLifetimeDays) < today;

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
