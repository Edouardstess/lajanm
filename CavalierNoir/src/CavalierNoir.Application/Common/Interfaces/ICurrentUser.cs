namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>Utilisateur à l'origine de la requête HTTP en cours.</summary>
public interface ICurrentUser
{
    int? UserId { get; }

    string? UserName { get; }

    string? DisplayName { get; }

    bool IsAuthenticated { get; }

    string? IpAddress { get; }

    string? CorrelationId { get; }

    bool IsInRole(string role);

    bool HasPermission(string permission);
}
