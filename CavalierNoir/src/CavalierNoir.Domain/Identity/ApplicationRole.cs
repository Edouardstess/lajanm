using Microsoft.AspNetCore.Identity;

namespace CavalierNoir.Domain.Identity;

/// <summary>
/// Rôle applicatif. Les permissions fines sont portées par des <c>Claims</c> de rôle
/// (voir <see cref="Permissions"/>) : le rôle regroupe, la claim autorise.
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }

    /// <summary>Rôle structurel du système, non supprimable depuis le back-office.</summary>
    public bool IsSystemRole { get; set; }

    /// <summary>Ordre d'affichage dans les écrans d'administration.</summary>
    public int DisplayOrder { get; set; }
}
