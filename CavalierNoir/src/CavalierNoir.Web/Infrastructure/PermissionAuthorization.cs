using CavalierNoir.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CavalierNoir.Web.Infrastructure;

/// <summary>
/// Exigence d'autorisation portant sur une permission fine
/// (« exercises.manage », « finance.view »…).
/// </summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

/// <summary>
/// Accorde l'accès si l'utilisateur porte la claim de permission demandée,
/// directement ou via l'un de ses rôles. Le super-administrateur passe toujours.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(Roles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(Permissions.ClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Fabrique à la volée une politique pour chaque permission connue, ce qui évite
/// d'enregistrer une trentaine de politiques à la main au démarrage.
/// Les autres noms de politique sont délégués au fournisseur par défaut.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (Permissions.All.Contains(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
