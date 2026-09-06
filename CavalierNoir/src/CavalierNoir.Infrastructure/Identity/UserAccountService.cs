using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Infrastructure.Identity;

/// <summary>
/// Adaptateur autour de <see cref="UserManager{TUser}"/> : la couche Application
/// n'a besoin que de l'affectation des rôles et ignore le reste d'Identity.
/// </summary>
public sealed class UserAccountService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserAccountService> logger) : IUserAccountService
{
    public async Task<bool> AddToRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        if (await userManager.IsInRoleAsync(user, role))
        {
            return true;
        }

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Impossible d'ajouter l'utilisateur {UserId} au rôle {Role} : {Errors}.",
                userId,
                role,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return result.Succeeded;
    }

    public async Task<bool> RemoveFromRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            return true;
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<bool> IsInRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, role);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }
}
