using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IAnalyticsService
    {
        Task<List<PoiAudioStat>> GetTopAudioStatsAsync(int count = 5);
        Task<List<UserHeatPoint>> GetUserVisitLocationsAsync();
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

        public async Task<List<UserHeatPoint>> GetUserVisitLocationsAsync()
        {
            return await _context.PoiVisits
                .AsNoTracking()
                .Where(v => v.Latitude.HasValue && v.Longitude.HasValue)
                .Select(v => new UserHeatPoint
                {
                    Lat = (double)v.Latitude.Value,
                    Lng = (double)v.Longitude.Value,
                    Count = 1
                })
                .ToListAsync();
        }
    }
}
