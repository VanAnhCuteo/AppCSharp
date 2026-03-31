using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IPoiPendingChangeService
    {
        Task<List<PoiPendingChange>> GetPendingChangesAsync();
        Task<PoiPendingChange?> GetByIdAsync(int id);
        Task<bool> CreatePendingChangeAsync(Poi poi, int userId);
        Task<bool> ApproveChangeAsync(int changeId);
        Task<bool> RejectChangeAsync(int changeId);
        Task<PoiPendingChange?> GetByPoiIdAsync(int poiId);
    }

    public class PoiPendingChangeService : IPoiPendingChangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public PoiPendingChangeService(ApplicationDbContext context, IActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PoiPendingChange>> GetPendingChangesAsync()
        {
            return await _context.PoiPendingChanges
                .Include(p => p.Poi)
                .Include(p => p.Category)
                .Include(p => p.Requester)
                .Where(p => p.Status == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<PoiPendingChange?> GetByIdAsync(int id)
        {
            return await _context.PoiPendingChanges
                .Include(p => p.Poi)
                .Include(p => p.Category)
                .Include(p => p.Requester)
                .FirstOrDefaultAsync(p => p.ChangeId == id);
        }

        public async Task<bool> CreatePendingChangeAsync(Poi poi, int userId)
        {
            // Check if there is already a pending change for this POI
            var existing = await _context.PoiPendingChanges
                .FirstOrDefaultAsync(p => p.PoiId == poi.PoiId && p.Status == "pending");

            if (existing != null)
            {
                // Update existing pending change
                existing.Name = poi.Name;
                existing.CategoryId = poi.CategoryId;
                existing.Address = poi.Address;
                existing.Latitude = poi.Latitude;
                existing.Longitude = poi.Longitude;
                existing.OpenTime = poi.OpenTime;
                existing.RangeMeters = poi.RangeMeters;
                existing.CreatedAt = DateTime.Now;
                existing.UserId = userId;
                _context.PoiPendingChanges.Update(existing);
            }
            else
            {
                // Create new
                var pending = new PoiPendingChange
                {
                    PoiId = poi.PoiId,
                    CategoryId = poi.CategoryId,
                    Name = poi.Name,
                    Address = poi.Address,
                    Latitude = poi.Latitude,
                    Longitude = poi.Longitude,
                    OpenTime = poi.OpenTime,
                    RangeMeters = poi.RangeMeters,
                    UserId = userId,
                    Status = "pending",
                    CreatedAt = DateTime.Now
                };
                _context.PoiPendingChanges.Add(pending);
            }

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                await _logger.LogAsync(userId, "Yêu cầu thay đổi", poi.Name, $"Đã gửi yêu cầu cập nhật thông tin cho {poi.Name}");
            }
            return result;
        }

        public async Task<bool> ApproveChangeAsync(int changeId)
        {
            var pending = await _context.PoiPendingChanges.FindAsync(changeId);
            if (pending == null) return false;

            var poi = await _context.Pois.FindAsync(pending.PoiId);
            if (poi == null) return false;

            // Apply changes to the main POI
            poi.Name = pending.Name ?? poi.Name;
            poi.CategoryId = pending.CategoryId ?? poi.CategoryId;
            poi.Address = pending.Address ?? poi.Address;
            poi.Latitude = pending.Latitude ?? poi.Latitude;
            poi.Longitude = pending.Longitude ?? poi.Longitude;
            poi.OpenTime = pending.OpenTime ?? poi.OpenTime;
            poi.RangeMeters = pending.RangeMeters;

            // Update status
            pending.Status = "approved";

            _context.Pois.Update(poi);
            _context.PoiPendingChanges.Update(pending);

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                await _logger.LogAsync(null, "Phê duyệt thay đổi", poi.Name, $"Admin đã phê duyệt thay đổi cho {poi.Name}");
            }
            return result;
        }

        public async Task<bool> RejectChangeAsync(int changeId)
        {
            var pending = await _context.PoiPendingChanges.FindAsync(changeId);
            if (pending == null) return false;

            pending.Status = "rejected";
            _context.PoiPendingChanges.Update(pending);

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var poi = await _context.Pois.FindAsync(pending.PoiId);
                await _logger.LogAsync(null, "Từ chối thay đổi", poi?.Name ?? "Unknown", $"Admin đã từ chối thay đổi cho {poi?.Name ?? "Unknown"}");
            }
            return result;
        }

        public async Task<PoiPendingChange?> GetByPoiIdAsync(int poiId)
        {
            return await _context.PoiPendingChanges
                .FirstOrDefaultAsync(p => p.PoiId == poiId && p.Status == "pending");
        }
    }
}
