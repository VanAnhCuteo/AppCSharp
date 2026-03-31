using FoodMapAdmin.Data;
using FoodMapAdmin.Models;

namespace FoodMapAdmin.Services
{
    public interface IActivityLogger
    {
        Task LogAsync(int? userId, string action, string target, string details = "");
        void LogToContext(int? userId, string action, string target, string details = "");
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
            try 
            {
                LogToContext(userId, action, target, details);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // We don't want a logging error to break the main application logic
                Console.WriteLine($"[Logger Error] {ex.Message}");
            }
        }

        public void LogToContext(int? userId, string action, string target, string details = "")
        {
            try 
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger Error] {ex.Message}");
            }
        }
    }
}
