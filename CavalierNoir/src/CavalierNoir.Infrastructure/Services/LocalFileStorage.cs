using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CavalierNoir.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CavalierNoir.Infrastructure.Services;

/// <summary>
/// Stockage sur le système de fichiers local, sous <c>wwwroot/uploads</c>.
/// Suffisant pour un déploiement mono-serveur ; remplaçable par un stockage
/// objet (MinIO / S3) en implémentant <see cref="IFileStorage"/>.
/// </summary>
public sealed partial class LocalFileStorage(
    string rootPath,
    string publicPrefix,
    ILogger<LocalFileStorage> logger) : IFileStorage
{
    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".odt", ".ods",
        ".txt", ".csv", ".pgn", ".mp4", ".webm"
    ];

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string fileName,
        string container,
        CancellationToken cancellationToken = default)
    {
        var safeContainer = SanitizeSegment(container);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Extension de fichier non autorisée : « {extension} ».");
        }

        var safeName = SanitizeSegment(Path.GetFileNameWithoutExtension(fileName));
        var truncated = safeName.Length > 60 ? safeName[..60] : safeName;
        var uniqueName = $"{truncated}-{Guid.NewGuid():N}{extension}";

        var directory = Path.Combine(rootPath, safeContainer);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, uniqueName);

        // Défense en profondeur : le chemin final doit rester sous la racine.
        var normalizedRoot = Path.GetFullPath(rootPath);
        var normalizedTarget = Path.GetFullPath(fullPath);
        if (!normalizedTarget.StartsWith(normalizedRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Chemin de destination invalide.");
        }

        long size;
        string checksum;

        await using (var output = File.Create(fullPath))
        {
            using var sha = SHA256.Create();
            await using var hashing = new CryptoStream(output, sha, CryptoStreamMode.Write, leaveOpen: true);
            await content.CopyToAsync(hashing, cancellationToken);
            await hashing.FlushFinalBlockAsync(cancellationToken);
            size = output.Length;
            checksum = Convert.ToHexString(sha.Hash ?? []).ToLowerInvariant();
        }

        var relativeUrl = $"{publicPrefix.TrimEnd('/')}/{safeContainer}/{uniqueName}";
        logger.LogInformation("Fichier enregistré : {Url} ({Size} octets).", relativeUrl, size);

        return new StoredFile(relativeUrl, uniqueName, size, ContentTypeFor(extension), checksum);
    }

    public Task<Stream?> OpenReadAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(relativeUrl);
        if (path is null || !File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(path));
    }

    public Task<bool> DeleteAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(relativeUrl);
        if (path is null || !File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    public bool Exists(string relativeUrl)
    {
        var path = ResolvePath(relativeUrl);
        return path is not null && File.Exists(path);
    }

    private string? ResolvePath(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return null;
        }

        var prefix = publicPrefix.TrimEnd('/');
        if (!relativeUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relative = relativeUrl[prefix.Length..].TrimStart('/');
        var candidate = Path.GetFullPath(Path.Combine(rootPath, relative));
        var root = Path.GetFullPath(rootPath);

        return candidate.StartsWith(root, StringComparison.Ordinal) ? candidate : null;
    }

    private static string SanitizeSegment(string value)
    {
        var cleaned = UnsafeCharacters().Replace(value, "-").Trim('-', '.');
        return string.IsNullOrWhiteSpace(cleaned) ? "fichier" : cleaned.ToLowerInvariant();
    }

    private static string ContentTypeFor(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".csv" => "text/csv",
        ".txt" or ".pgn" => "text/plain",
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        _ => "application/octet-stream"
    };

    [GeneratedRegex("[^a-zA-Z0-9._-]+")]
    private static partial Regex UnsafeCharacters();
}
