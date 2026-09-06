namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>
/// Opérations d'ASP.NET Core Identity dont la couche Application a besoin
/// (affectation de rôles) sans dépendre de <c>UserManager&lt;T&gt;</c>.
/// </summary>
public interface IUserAccountService
{
    Task<bool> AddToRoleAsync(int userId, string role, CancellationToken cancellationToken = default);

    Task<bool> RemoveFromRoleAsync(int userId, string role, CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(int userId, CancellationToken cancellationToken = default);
}
