using System.Security.Claims;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Identity;

namespace CavalierNoir.Web.Infrastructure;

/// <summary>
/// Utilisateur de la requête HTTP courante, lu depuis le ticket
/// d'authentification. Aucun accès à la base : tout provient des claims.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.Identity?.Name;

    public string? DisplayName =>
        Principal?.FindFirstValue("display_name") ?? UserName;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <summary>Identifiant de corrélation, aligné sur le <c>TraceIdentifier</c> ASP.NET Core.</summary>
    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public bool HasPermission(string permission) =>
        Principal?.HasClaim(Permissions.ClaimType, permission) ?? false;
}
