using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface INotificationService
    {
        Task<List<AppNotification>> GetRecentNotificationsAsync(int userId, int limit = 10);
        Task<int> GetUnreadCountAsync(int userId);
        Task SendNotificationAsync(int userId, string title, string message, string type = "info", string? category = null);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public NotificationService(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<AppNotification>> GetRecentNotificationsAsync(int userId, int limit = 10)
        {
            using var _context = _factory.CreateDbContext();
            return await _context.AppNotifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            using var _context = _factory.CreateDbContext();
            return await _context.AppNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type = "info", string? category = null)
        {
            using var _context = _factory.CreateDbContext();
            var notification = new AppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Category = category,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.AppNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            using var _context = _factory.CreateDbContext();
            var notification = await _context.AppNotifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            using var _context = _factory.CreateDbContext();
            var unread = await _context.AppNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
