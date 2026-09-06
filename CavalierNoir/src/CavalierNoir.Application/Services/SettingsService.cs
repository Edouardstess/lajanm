using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Club;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <summary>
/// Paramètres modifiables en production (table <c>SystemSettings</c>).
/// Les valeurs sensibles ne sont jamais renvoyées telles quelles à l'interface.
/// </summary>
public sealed class SettingsService(IApplicationDbContext context, IDateTimeProvider clock)
{
    public async Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        await context.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

    public async Task<string> GetAsync(string key, string fallback, CancellationToken ct = default) =>
        await GetAsync(key, ct) ?? fallback;

    public async Task<bool> GetBoolAsync(string key, bool fallback = false, CancellationToken ct = default)
    {
        var value = await GetAsync(key, ct);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async Task<int> GetIntAsync(string key, int fallback = 0, CancellationToken ct = default)
    {
        var value = await GetAsync(key, ct);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async Task<IReadOnlyList<SystemSetting>> GetGroupAsync(string group, CancellationToken ct = default) =>
        await context.SystemSettings
            .AsNoTracking()
            .Where(s => s.Group == group)
            .OrderBy(s => s.Key)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(CancellationToken ct = default) =>
        await context.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Key)
            .ToListAsync(ct);

    public async Task SetAsync(string key, string? value, int? updatedById, CancellationToken ct = default)
    {
        var setting = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, ct);

        if (setting is null)
        {
            setting = new SystemSetting { Key = key };
            context.SystemSettings.Add(setting);
        }

        setting.Value = value;
        setting.UpdatedAt = clock.UtcNow;
        setting.UpdatedById = updatedById;

        await context.SaveChangesAsync(ct);
    }

    /// <summary>Enregistre plusieurs paramètres en une seule transaction.</summary>
    public async Task SetManyAsync(
        IReadOnlyDictionary<string, string?> values,
        int? updatedById,
        CancellationToken ct = default)
    {
        var keys = values.Keys.ToList();
        var existing = await context.SystemSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(ct);

        var now = clock.UtcNow;

        foreach (var (key, value) in values)
        {
            var setting = existing.FirstOrDefault(s => s.Key == key);
            if (setting is null)
            {
                setting = new SystemSetting { Key = key };
                context.SystemSettings.Add(setting);
            }

            setting.Value = value;
            setting.UpdatedAt = now;
            setting.UpdatedById = updatedById;
        }

        await context.SaveChangesAsync(ct);
    }
}
