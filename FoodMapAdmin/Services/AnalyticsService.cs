using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IAnalyticsService
    {
        Task<List<PoiAudioStat>> GetTopAudioStatsAsync(int count = 5);
        Task<List<UserHeatPoint>> GetActiveUserLocationsAsync();
        Task<MonitoringStats> GetMonitoringStatsAsync();
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PoiAudioStat>> GetTopAudioStatsAsync(int count = 5)
        {
            // Join Pois with PoiAudioLogs to aggregate stats
            var stats = await _context.Pois
                .Select(p => new PoiAudioStat
                {
                    PoiId = p.PoiId,
                    PoiName = p.Name,
                    TotalListens = _context.PoiAudioLogs.Count(l => l.PoiId == p.PoiId),
                    AverageDurationSeconds = _context.PoiAudioLogs
                        .Where(l => l.PoiId == p.PoiId)
                        .Select(l => (double?)l.DurationSeconds)
                        .Average() ?? 0
                })
                .OrderByDescending(s => s.TotalListens)
                .Take(count)
                .ToListAsync();

            return stats;
        }

        public async Task<List<UserHeatPoint>> GetActiveUserLocationsAsync()
        {
            var activeThreshold = DateTime.Now.AddSeconds(-21);
            return await _context.UserLocations
                .AsNoTracking()
                .Where(u => u.LastActive > activeThreshold)
                .Select(u => new UserHeatPoint
                {
                    Lat = (double)u.Latitude,
                    Lng = (double)u.Longitude,
                    Count = 1
                })
                .ToListAsync();
        }

        public async Task<MonitoringStats> GetMonitoringStatsAsync()
        {
            var activeThreshold = DateTime.Now.AddSeconds(-21);
            var activeRecords = await _context.UserLocations
                .AsNoTracking()
                .Where(u => u.LastActive > activeThreshold)
                .ToListAsync();

            var stats = new MonitoringStats
            {
                ActiveUsers = activeRecords.Count,
                ActiveListeners = activeRecords.Count(r => r.IsListeningAudio),
                PoiListeners = activeRecords
                    .Where(r => r.IsListeningAudio && r.ListeningPoiId.HasValue)
                    .GroupBy(r => r.ListeningPoiId.Value)
                    .Select(g => new ActivePoiListener
                    {
                        PoiId = g.Key,
                        ListenerCount = g.Count(),
                        PoiName = _context.Pois.FirstOrDefault(p => p.PoiId == g.Key)?.Name ?? $"POI #{g.Key}"
                    })
                    .ToList()
            };

            return stats;
        }
    }
}
