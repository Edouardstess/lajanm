namespace CavalierNoir.Web.Infrastructure;

/// <summary>
/// Ajoute les en-têtes de sécurité HTTP décrits au §2.2 du référentiel :
/// politique de sécurité de contenu, interdiction d'encadrement, contrôle du
/// référent et désactivation des API sensibles du navigateur.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Politique de sécurité de contenu. Les styles en ligne sont tolérés pour
    /// l'échiquier, dont les cases sont positionnées dynamiquement ; aucun script
    /// en ligne n'est autorisé.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";

        // L'en-tête révélant le serveur n'apporte rien à l'utilisateur.
        headers.Remove("Server");

        await next(context);
    }
}

/// <summary>Extension d'enregistrement du middleware.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
