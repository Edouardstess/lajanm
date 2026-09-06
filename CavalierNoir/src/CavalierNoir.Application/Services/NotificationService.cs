using CavalierNoir.Application.Common.Interfaces;
using CavalierNoir.Domain.Enums;
using CavalierNoir.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace CavalierNoir.Application.Services;

/// <inheritdoc cref="INotificationService" />
public sealed class NotificationService(IApplicationDbContext context, IDateTimeProvider clock)
    : INotificationService
{
    public async Task NotifyAsync(
        int userId,
        string title,
        string message,
        string? url = null,
        string? icon = null,
        int priority = 3,
        NotificationChannel channel = NotificationChannel.InApp,
        CancellationToken cancellationToken = default)
    {
        context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Url = url,
            Icon = icon,
            Priority = priority,
            Channel = channel,
            CreatedAt = clock.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task NotifyRoleAsync(
        string role,
        string title,
        string message,
        string? url = null,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        var userIds = await (
            from user in context.Users
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join appRole in context.Roles on userRole.RoleId equals appRole.Id
            where appRole.Name == role && user.IsActive
            select user.Id).Distinct().ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        var now = clock.UtcNow;
        foreach (var userId in userIds)
        {
            context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = url,
                Icon = icon,
                Priority = 2,
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default) =>
        context.Notifications.CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);

    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.MarkAsRead(clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        var now = clock.UtcNow;
        foreach (var notification in notifications)
        {
            notification.MarkAsRead(now);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
