using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IDashboardService
    {
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();

            // 1. Lấy thống kê cơ bản
            stats.TotalPois = await _context.Pois.CountAsync();

            stats.TotalUsers = await _context.Users.CountAsync();
            stats.TotalAdmins = await _context.Users.CountAsync(u => u.Role == "admin");
            stats.TotalOwners = await _context.Users.CountAsync(u => u.Role == "CNH");
            stats.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "user");

            // 2. Lấy hoạt động gần đây từ bảng activity_logs
            stats.RecentActivities = await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .Select(a => new ActivityDto
                {
                    ActionType = a.ActionType,
                    UserName = a.User != null ? a.User.Username : "Hệ thống",
                    TargetName = a.TargetName ?? "N/A",
                    Time = a.CreatedAt
                })
                .ToListAsync();

            return stats;
        }
    }
}
