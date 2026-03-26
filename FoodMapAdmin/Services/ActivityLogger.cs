using FoodMapAdmin.Data;
using FoodMapAdmin.Models;

namespace FoodMapAdmin.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(int? userId, string action, string target, string details = "");
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int? userId, string action, string target, string details = "")
        {
            var log = new ActivityLog
            {
                UserId = userId,
                ActionType = action,
                TargetName = target,
                Details = details,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
