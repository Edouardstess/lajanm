using System.Text;
using CavalierNoir.Application.Common;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Learning;
using CavalierNoir.Domain.Memberships;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Fabrique des courriels transactionnels. Les gabarits sont volontairement
/// simples (HTML en tableau, styles en ligne) pour rester lisibles dans tous les
/// clients de messagerie, y compris sur mobile en connexion lente.
/// </summary>
public sealed class EmailTemplateFactory(IOptions<SiteOptions> siteOptions)
{
    private readonly SiteOptions _site = siteOptions.Value;

    public EmailMessage BuildDailyExercise(
        string to,
        string? recipientName,
        Exercise exercise,
        DailyExercise daily,
        string token)
    {
        var url = $"{_site.BaseUrl.TrimEnd('/')}/Exercices/Quotidien/{token}";
        var board = ChessBoardRenderer.ToHtml(exercise.Fen);
        var sideToMove = exercise.SideToMoveLabel;

        var body = new StringBuilder();
        body.Append($"<p>Bonjour {WebEncode(recipientName ?? "cher membre")},</p>");
        body.Append("<p>Voici votre exercice du jour :</p>");
        body.Append($"<p style=\"text-align:center;\">{board}</p>");
        body.Append($"<p style=\"text-align:center;font-weight:600;\">{WebEncode(sideToMove)}</p>");
        body.Append($"<p style=\"text-align:center;\">Thème : {WebEncode(exercise.Theme.ToString())} — ");
        body.Append($"Difficulté : {(int)exercise.Difficulty}/5</p>");
        body.Append(Button(url, "Résoudre l'exercice"));
        body.Append("<p style=\"font-size:13px;color:#666;\">Ce lien est personnel : il enregistre votre "
                    + "résultat et met à jour votre série de réussites.</p>");

        var text = new StringBuilder();
        text.AppendLine($"Bonjour {recipientName ?? "cher membre"},");
        text.AppendLine();
        text.AppendLine("Votre exercice du jour :");
        text.AppendLine();
        text.AppendLine(ChessBoardRenderer.ToText(exercise.Fen));
        text.AppendLine(sideToMove);
        text.AppendLine();
        text.AppendLine($"Résoudre : {url}");

        return new EmailMessage
        {
            To = to,
            ToName = recipientName,
            Subject = $"♞ Exercice du jour — {exercise.Title}",
            HtmlBody = Layout($"Exercice du {daily.ScheduledOn:dd/MM/yyyy}", body.ToString()),
            TextBody = text.ToString(),
            Template = "daily-exercise",
            DailyExerciseId = daily.Id
        };
    }

    public EmailMessage Welcome(string to, string recipientName, string memberNumber)
    {
        var body = new StringBuilder();
        body.Append($"<p>Bonjour {WebEncode(recipientName)},</p>");
        body.Append("<p>Votre adhésion au club <strong>Cavalier Noir</strong> est active. "
                    + $"Votre numéro d'adhérent est <strong>{WebEncode(memberNumber)}</strong>.</p>");
        body.Append("<p>Vous avez désormais accès à la bibliothèque d'exercices, aux tournois, "
                    + "aux forums et à l'ensemble des ressources pédagogiques du club.</p>");
        body.Append(Button($"{_site.BaseUrl.TrimEnd('/')}/Membre", "Accéder à mon espace"));

        return new EmailMessage
        {
            To = to,
            ToName = recipientName,
            Subject = "Bienvenue au Cavalier Noir",
            HtmlBody = Layout("Bienvenue !", body.ToString()),
            TextBody = $"Bonjour {recipientName}, votre adhésion est active (n° {memberNumber}).",
            Template = "welcome"
        };
    }

    public EmailMessage RenewalReminder(string to, string recipientName, Membership membership)
    {
        var body = new StringBuilder();
        body.Append($"<p>Bonjour {WebEncode(recipientName)},</p>");
        body.Append($"<p>Votre adhésion arrive à échéance le "
                    + $"<strong>{membership.Period.End:dd/MM/yyyy}</strong>.</p>");
        body.Append("<p>Renouvelez-la dès maintenant pour conserver sans interruption l'accès "
                    + "à l'espace membre, aux tournois et aux exercices.</p>");
        body.Append(Button($"{_site.BaseUrl.TrimEnd('/')}/Membre/Adhesion", "Renouveler mon adhésion"));
        body.Append($"<p style=\"font-size:13px;color:#666;\">Passé cette date, un délai de grâce de "
                    + $"{Membership.GracePeriodDays} jours s'applique avant la suspension du compte.</p>");

        return new EmailMessage
        {
            To = to,
            ToName = recipientName,
            Subject = "Votre adhésion arrive à échéance",
            HtmlBody = Layout("Renouvellement d'adhésion", body.ToString()),
            TextBody = $"Votre adhésion expire le {membership.Period.End:dd/MM/yyyy}.",
            Template = "membership-renewal"
        };
    }

    public EmailMessage NewsletterConfirmation(string to, string confirmationUrl)
    {
        var body = new StringBuilder();
        body.Append("<p>Vous avez demandé à recevoir la lettre d'information du Cavalier Noir.</p>");
        body.Append("<p>Confirmez votre abonnement en cliquant sur le bouton ci-dessous. "
                    + "Sans confirmation, aucun message ne vous sera envoyé.</p>");
        body.Append(Button(confirmationUrl, "Confirmer mon abonnement"));

        return new EmailMessage
        {
            To = to,
            Subject = "Confirmez votre abonnement",
            HtmlBody = Layout("Confirmation d'abonnement", body.ToString()),
            TextBody = $"Confirmez votre abonnement : {confirmationUrl}",
            Template = "newsletter-confirmation"
        };
    }

    public EmailMessage PairingPublished(
        string to,
        string recipientName,
        string tournamentTitle,
        int roundNumber,
        int board,
        string colour,
        string opponentName,
        string url)
    {
        var body = new StringBuilder();
        body.Append($"<p>Bonjour {WebEncode(recipientName)},</p>");
        body.Append($"<p>Les appariements de la ronde {roundNumber} du tournoi "
                    + $"« {WebEncode(tournamentTitle)} » sont publiés.</p>");
        body.Append($"<p>Échiquier <strong>{board}</strong> — vous jouez avec les "
                    + $"<strong>{WebEncode(colour)}</strong> contre {WebEncode(opponentName)}.</p>");
        body.Append(Button(url, "Voir la feuille de ronde"));

        return new EmailMessage
        {
            To = to,
            ToName = recipientName,
            Subject = $"Ronde {roundNumber} — {tournamentTitle}",
            HtmlBody = Layout("Appariements publiés", body.ToString()),
            TextBody = $"Ronde {roundNumber} : échiquier {board}, {colour}, contre {opponentName}. {url}",
            Template = "tournament-pairing"
        };
    }

    /// <summary>Habillage commun : en-tête, contenu, pied de page conforme au RGPD.</summary>
    public string Layout(string title, string content, string? unsubscribeUrl = null)
    {
        var footer = unsubscribeUrl is null
            ? string.Empty
            : $"<p style=\"font-size:12px;color:#888;\">Vous ne souhaitez plus recevoir ces messages ? "
              + $"<a href=\"{unsubscribeUrl}\" style=\"color:#888;\">Se désabonner</a>.</p>";

        return $"""
                <div style="margin:0;padding:24px 0;background:#f5f0eb;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#1a1a1a;">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                    <tr><td align="center">
                      <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border:1px solid #e5ded5;">
                        <tr>
                          <td style="background:#1a1a1a;padding:20px 24px;">
                            <span style="color:#d4af37;font-size:22px;font-weight:700;letter-spacing:.04em;">♞ {WebEncode(_site.Name)}</span><br>
                            <span style="color:#cfc7bb;font-size:13px;">{WebEncode(_site.Tagline)}</span>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:24px;">
                            <h1 style="margin:0 0 16px;font-size:20px;color:#1a1a1a;">{WebEncode(title)}</h1>
                            {content}
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:16px 24px;background:#f5f0eb;font-size:12px;color:#777;">
                            {WebEncode(_site.Name)} — {WebEncode(_site.Address)}<br>
                            <a href="{_site.BaseUrl}" style="color:#777;">{WebEncode(_site.BaseUrl)}</a>
                            {footer}
                          </td>
                        </tr>
                      </table>
                    </td></tr>
                  </table>
                </div>
                """;
    }

    private static string Button(string url, string label) =>
        $"<p style=\"text-align:center;margin:24px 0;\">"
        + $"<a href=\"{url}\" style=\"display:inline-block;background:#d4af37;color:#1a1a1a;"
        + $"text-decoration:none;padding:12px 24px;font-weight:600;border-radius:6px;\">{WebEncode(label)}</a></p>";

    private static string WebEncode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
