using System.Globalization;
using System.Threading.RateLimiting;
using CavalierNoir.Application;
using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Application.Common.Models;
using CavalierNoir.Domain.Identity;
using CavalierNoir.Infrastructure;
using CavalierNoir.Infrastructure.Persistence;
using CavalierNoir.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration et couches applicatives
// ---------------------------------------------------------------------------
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------------------------------------------------------------------------
// Identité : mots de passe, verrouillage, cookies
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Politique de mot de passe (§12.2 du DAL) : 12 caractères minimum.
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 4;

        // Verrouillage : 5 échecs bloquent le compte 15 minutes.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Compte/Connexion";
    options.LogoutPath = "/Compte/Deconnexion";
    options.AccessDeniedPath = "/Compte/AccesRefuse";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.Name = "CavalierNoir.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ---------------------------------------------------------------------------
// Autorisation : une politique nommée par permission, adossée aux claims de rôle
// ---------------------------------------------------------------------------
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("EspaceMembre", policy => policy.RequireRole(Roles.MemberRoles.Split(',')))
    .AddPolicy("EspaceAdmin", policy => policy.RequireRole(Roles.StaffRoles.Split(',')));

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

// ---------------------------------------------------------------------------
// MVC, localisation, compression, limitation de débit
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<DomainExceptionFilter>();
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supported = new[]
    {
        new CultureInfo("fr-FR"),
        new CultureInfo("ht"),
        new CultureInfo("en")
    };

    options.DefaultRequestCulture = new RequestCulture("fr-FR");
    options.SupportedCultures = supported;
    options.SupportedUICultures = supported;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 100 requêtes par minute et par adresse IP sur l'ensemble du site.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // 20 requêtes par minute sur les points d'entrée sensibles.
    options.AddPolicy("authentification", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Formulaires publics : 5 envois par minute.
    options.AddPolicy("formulaire", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "inconnu",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "CavalierNoir.Antiforgery";
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.AddHealthChecks();

builder.Services.AddResponseCaching();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline HTTP
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Erreur");
    app.UseStatusCodePagesWithReExecute("/Erreur/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseSecurityHeaders();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        // Les fichiers statiques versionnés peuvent être mis en cache longtemps.
        context.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=604800";
    }
});

app.UseRequestLocalization();
app.UseRouting();
app.UseRateLimiter();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=TableauDeBord}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------------------------------------------------------------------
// Préparation de la base au démarrage
// ---------------------------------------------------------------------------
await app.Services.InitializeDatabaseAsync();

app.Run();

/// <summary>
/// Exposé pour permettre aux tests d'intégration d'instancier l'application
/// avec <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program
{
}
