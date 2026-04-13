using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IPoiPendingChangeService
    {
        Task<List<PoiPendingChange>> GetPendingChangesAsync();
        Task<PoiPendingChange?> GetByIdAsync(int id);
        Task<bool> CreatePendingChangeAsync(Poi poi, int userId, string changeType = "update");
        Task<bool> ApproveChangeAsync(int changeId);
        Task<bool> RejectChangeAsync(int changeId);
        Task<PoiPendingChange?> GetByPoiIdAsync(int poiId);
        Task<List<PoiPendingChange>> GetChangesByUserIdAsync(int userId);
        Task<bool> CancelAsync(int id);
    }

    public class PoiPendingChangeService : IPoiPendingChangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly INotificationService _notificationService;

        public PoiPendingChangeService(ApplicationDbContext context, IActivityLogger logger, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<List<PoiPendingChange>> GetPendingChangesAsync()
        {
            return await _context.PoiPendingChanges
                .Include(p => p.Poi)
                    .ThenInclude(p => p.Category)
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
                    .ThenInclude(p => p.Category)
                .Include(p => p.Category)
                .Include(p => p.Requester)
                .FirstOrDefaultAsync(p => p.ChangeId == id);
        }

        public async Task<bool> CreatePendingChangeAsync(Poi poi, int userId, string changeType = "update")
        {
            if (changeType == "update")
            {
                // Use AsNoTracking() and avoid navigation properties during check
                var existing = await _context.PoiPendingChanges
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PoiId == poi.PoiId && p.Status == "pending" && p.ChangeType == "update");

                if (existing != null)
                {
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
                    var pending = new PoiPendingChange
                    {
                        PoiId = poi.PoiId,
                        ChangeType = "update",
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
            }
            else
            {
                // For 'create' or 'delete'
                var pending = new PoiPendingChange
                {
                    PoiId = changeType == "create" ? null : poi.PoiId,
                    ChangeType = changeType,
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
                string actionLabel = changeType == "create" ? "Yêu cầu thêm mới" : (changeType == "delete" ? "Yêu cầu xóa" : "Yêu cầu thay đổi");
                await _logger.LogAsync(userId, actionLabel, poi.Name, $"Đã gửi {actionLabel.ToLower()} cho {poi.Name}");
            }
            return result;
        }

        public async Task<bool> ApproveChangeAsync(int changeId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try 
            {
                var pending = await _context.PoiPendingChanges
                    .Include(p => p.Poi)
                    .FirstOrDefaultAsync(p => p.ChangeId == changeId);
                
                if (pending == null) return false;

                string actionLabel = "";
                string poiName = pending.Name ?? "Quán ăn";

                if (pending.ChangeType == "create")
                {
                    // Case 1: Approve addition of a new POI
                    var newPoi = new Poi
                    {
                        Name = pending.Name ?? "",
                        CategoryId = pending.CategoryId,
                        Address = pending.Address,
                        Latitude = pending.Latitude,
                        Longitude = pending.Longitude,
                        OpenTime = pending.OpenTime,
                        RangeMeters = pending.RangeMeters,
                        UserId = pending.UserId, // The original requester becomes the owner
                        CreatedAt = DateTime.Now
                    };
                    
                    _context.Pois.Add(newPoi);
                    await _context.SaveChangesAsync(); // Save POI first to generate Its ID

                    // Crucial: Update the pending record with the newly created PoiId
                    pending.PoiId = newPoi.PoiId; 
                    actionLabel = "Phê duyệt thêm mới";
                }
                else if (pending.ChangeType == "delete")
                {
                    // Case 2: Approve deletion of an existing POI
                    if (pending.PoiId.HasValue)
                    {
                        var poi = await _context.Pois
                            .Include(p => p.Images)
                            .Include(p => p.Guides)

                            .FirstOrDefaultAsync(p => p.PoiId == pending.PoiId.Value);

                        if (poi != null) 
                        {
                            // Clear related data first (though Cascade Delete might handle this, explicit is safer)
                            if (poi.Images?.Any() == true) _context.PoiImages.RemoveRange(poi.Images);
                            if (poi.Guides?.Any() == true) _context.PoiGuides.RemoveRange(poi.Guides);

                            
                            _context.Pois.Remove(poi);
                            actionLabel = "Phê duyệt xóa";
                        }
                    }
                }
                else // default is "update"
                {
                    // Case 3: Approve changes to an existing POI
                    if (pending.PoiId.HasValue)
                    {
                        var poi = await _context.Pois.FindAsync(pending.PoiId.Value);
                        if (poi != null)
                        {
                            poi.Name = pending.Name ?? poi.Name;
                            poi.CategoryId = pending.CategoryId ?? poi.CategoryId;
                            poi.Address = pending.Address ?? poi.Address;
                            poi.Latitude = pending.Latitude ?? poi.Latitude;
                            poi.Longitude = pending.Longitude ?? poi.Longitude;
                            poi.OpenTime = pending.OpenTime ?? poi.OpenTime;
                            poi.RangeMeters = pending.RangeMeters;
                            
                            _context.Pois.Update(poi);
                            actionLabel = "Phê duyệt thay đổi";
                        }
                    }
                }

                // Mark the request as approved
                pending.Status = "approved";
                _context.PoiPendingChanges.Update(pending);

                // Notifications
                string actionVn = pending.ChangeType == "create" ? "THÊM MỚI" : (pending.ChangeType == "delete" ? "XÓA BỎ" : "CẬP NHẬT");
                await _notificationService.SendNotificationAsync(
                    pending.UserId ?? 0, 
                    "Cửa hàng được phê duyệt", 
                    $"Yêu cầu {actionVn} cho quán '{poiName}' đã được Admin phê duyệt.", 
                    "success", 
                    "poi"
                );

                // Log the action
                string logDetails = $"Admin đã phê duyệt yêu cầu {pending.ChangeType} cho quán {poiName}";
                _logger.LogToContext(null, actionLabel != "" ? actionLabel : "Phê duyệt", poiName, logDetails);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Capture inner exception for more details (useful for DB constraint errors)
                var errorMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                Console.WriteLine($"[Approve Error] {errorMsg}");
                throw new Exception($"Lỗi thực thi database: {errorMsg}", ex);
            }
        }

        public async Task<bool> RejectChangeAsync(int changeId)
        {
            var pending = await _context.PoiPendingChanges.FindAsync(changeId);
            if (pending == null) return false;

            pending.Status = "rejected";
            _context.PoiPendingChanges.Update(pending);

            // Notifications
            string actionVn = pending.ChangeType == "create" ? "THÊM MỚI" : (pending.ChangeType == "delete" ? "XÓA BỎ" : "CẬP NHẬT");
            await _notificationService.SendNotificationAsync(
                pending.UserId ?? 0, 
                "Yêu cầu bị từ chối", 
                $"Rất tiếc, yêu cầu {actionVn} cửa hàng '{pending.Name}' đã bị từ chối.", 
                "alert", 
                "poi"
            );

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                string poiName = pending.Name ?? "Quán ăn";
                await _logger.LogAsync(null, "Từ chối thay đổi", poiName, $"Admin đã từ chối yêu cầu {pending.ChangeType} cho {poiName}");
            }
            return result;
        }


        public async Task<PoiPendingChange?> GetByPoiIdAsync(int poiId)
        {
            return await _context.PoiPendingChanges
                .FirstOrDefaultAsync(p => p.PoiId == poiId && p.Status == "pending");
        }

        public async Task<List<PoiPendingChange>> GetChangesByUserIdAsync(int userId)
        {
            return await _context.PoiPendingChanges
                .Include(p => p.Poi)
                .Include(p => p.Category)
                .Where(p => p.UserId == userId && p.Status == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelAsync(int id)
        {
            var p = await _context.PoiPendingChanges.FindAsync(id);
            if (p != null)
            {
                _context.PoiPendingChanges.Remove(p);
                return await _context.SaveChangesAsync() > 0;
            }
            return false;
        }
    }
}
