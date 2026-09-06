using CavalierNoir.Domain.Enums;

namespace CavalierNoir.Application.Common.Interfaces;

/// <summary>Création des notifications internes affichées dans la cloche du site.</summary>
public interface INotificationService
{
    Task NotifyAsync(
        int userId,
        string title,
        string message,
        string? url = null,
        string? icon = null,
        int priority = 3,
        NotificationChannel channel = NotificationChannel.InApp,
        CancellationToken cancellationToken = default);

    /// <summary>Notifie tous les porteurs d'un rôle (secrétaires, trésoriers…).</summary>
    Task NotifyRoleAsync(
        string role,
        string title,
        string message,
        string? url = null,
        string? icon = null,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}
