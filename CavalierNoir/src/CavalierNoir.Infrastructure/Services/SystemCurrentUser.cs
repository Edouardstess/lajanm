using CavalierNoir.Application.Common.Interfaces;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Utilisateur « système » utilisé hors requête HTTP : tâches de fond, semis de
/// données, tests. Les écritures qu'il produit sont tracées sous ce nom.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public int? UserId => null;

    public string? UserName => "système";

    public string? DisplayName => "Tâche automatique";

    public bool IsAuthenticated => false;

    public string? IpAddress => null;

    public string? CorrelationId => null;

    public bool IsInRole(string role) => false;

    public bool HasPermission(string permission) => false;
}
