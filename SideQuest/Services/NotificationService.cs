using Microsoft.EntityFrameworkCore;
using SideQuest.Contracts;
using SideQuest.Data;

namespace SideQuest.Services
{
    public interface INotificationService
    {
        Task<ServiceResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync(string userId, bool unreadOnly);

        Task<ServiceResult<NotificationResponse>> MarkReadAsync(string userId, int notificationId);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync(string userId, bool unreadOnly)
        {
            var query = _context.Notifications
                .Where(notification => notification.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(notification => !notification.IsRead);
            }

            var notifications = await query
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(100)
                .ToListAsync();

            return ServiceResult<IReadOnlyList<NotificationResponse>>.Success(
                notifications.Select(notification => notification.ToResponse()).ToList());
        }

        public async Task<ServiceResult<NotificationResponse>> MarkReadAsync(string userId, int notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(existingNotification =>
                    existingNotification.Id == notificationId &&
                    existingNotification.UserId == userId);

            if (notification is null)
            {
                return ServiceResult<NotificationResponse>.NotFound("Notification was not found.");
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return ServiceResult<NotificationResponse>.Success(notification.ToResponse());
        }
    }
}
