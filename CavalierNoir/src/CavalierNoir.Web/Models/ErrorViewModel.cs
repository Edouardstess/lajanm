namespace CavalierNoir.Web.Models;

/// <summary>Contexte d'affichage de la page d'erreur.</summary>
public sealed class ErrorViewModel
{
    public int StatusCode { get; init; } = 500;

    public string Title { get; init; } = "Une erreur est survenue";

    public string Message { get; init; } =
        "Une erreur inattendue s'est produite. L'incident a été enregistré.";

    /// <summary>Identifiant de corrélation à communiquer au support.</summary>
    public string? RequestId { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
