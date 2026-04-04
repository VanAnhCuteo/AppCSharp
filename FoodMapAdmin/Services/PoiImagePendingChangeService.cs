using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IPoiImagePendingChangeService
    {
        Task<List<PoiImagePendingChange>> GetPendingChangesAsync();
        Task<bool> CreatePendingChangeAsync(PoiImagePendingChange change);
        Task<bool> ApproveChangeAsync(int changeId);
        Task<bool> RejectChangeAsync(int changeId);
    }

    public class PoiImagePendingChangeService : IPoiImagePendingChangeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly IConfiguration _configuration;
        private readonly string _storagePath;

        public PoiImagePendingChangeService(ApplicationDbContext context, IActivityLogger logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _storagePath = _configuration["ImageStoragePath"] ?? @"d:\MapApp\FoodMapApp\Resources\Raw\images";
        }

        public async Task<List<PoiImagePendingChange>> GetPendingChangesAsync()
        {
            // Fetch everything but handle Joins carefully
            var list = await _context.PoiImagePendingChanges
                .Include(p => p.Poi)
                .Include(p => p.Requester)
                .Include(p => p.OriginalImage) // This is now a LEFT JOIN in EF because image_id is nullable
                .Where(p => p.Status == "pending")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            
            return list;
        }

        public async Task<bool> CreatePendingChangeAsync(PoiImagePendingChange change)
        {
            _context.PoiImagePendingChanges.Add(change);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var poiName = (await _context.Pois.FindAsync(change.PoiId))?.Name ?? "N/A";
                string actionLabel = change.ChangeType switch {
                    "add" => "Yêu cầu thêm ảnh",
                    "delete" => "Yêu cầu xóa ảnh",
                    "update" => "Yêu cầu sửa ảnh",
                    _ => "Yêu cầu hình ảnh"
                };
                await _logger.LogAsync(change.UserId, actionLabel, poiName, $"Đã gửi {actionLabel.ToLower()} cho {poiName}");
            }
            return result;
        }

        public async Task<bool> ApproveChangeAsync(int changeId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var pending = await _context.PoiImagePendingChanges.FindAsync(changeId);
                if (pending == null) return false;

                var poi = await _context.Pois.FindAsync(pending.PoiId);
                string poiName = poi?.Name ?? "N/A";

                if (pending.ChangeType == "add")
                {
                    // Approve Addition
                    var newImage = new PoiImage
                    {
                        PoiId = pending.PoiId,
                        ImageUrl = pending.ImageUrl
                    };
                    _context.PoiImages.Add(newImage);
                    _logger.LogToContext(null, "Phê duyệt thêm ảnh", poiName, $"Admin đã phê duyệt thêm ảnh mới cho {poiName}");
                }
                else if (pending.ChangeType == "delete")
                {
                    // Approve Deletion
                    if (pending.ImageId.HasValue)
                    {
                        var image = await _context.PoiImages.FindAsync(pending.ImageId.Value);
                        if (image != null)
                        {
                            // Delete physical file
                            DeletePhysicalFile(image.ImageUrl);
                            _context.PoiImages.Remove(image);
                            _logger.LogToContext(null, "Phê duyệt xóa ảnh", poiName, $"Admin đã phê duyệt xóa một ảnh của {poiName}");
                        }
                    }
                }
                else if (pending.ChangeType == "update")
                {
                    // Approve Update
                    if (pending.ImageId.HasValue)
                    {
                        var image = await _context.PoiImages.FindAsync(pending.ImageId.Value);
                        if (image != null)
                        {
                            // if ImageUrl has changed, it means a new file was uploaded
                            if (image.ImageUrl != pending.ImageUrl) 
                            {
                                // Delete OLD physical file
                                DeletePhysicalFile(image.ImageUrl);
                                image.ImageUrl = pending.ImageUrl;
                            }
                            
                            // Update POI assignment (even if image file didn't change)
                            image.PoiId = pending.PoiId;
                            
                            _context.PoiImages.Update(image);
                            _logger.LogToContext(null, "Phê duyệt sửa ảnh", poiName, $"Admin đã phê duyệt cập nhật ảnh mới/thông tin cho {poiName}");
                        }
                    }
                }

                pending.Status = "approved";
                _context.PoiImagePendingChanges.Update(pending);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error approving image change: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RejectChangeAsync(int changeId)
        {
            var pending = await _context.PoiImagePendingChanges.FindAsync(changeId);
            if (pending == null) return false;

            // If it was an "add" request, we might want to delete the uploaded file since it was rejected
            if (pending.ChangeType == "add" || pending.ChangeType == "update")
            {
                DeletePhysicalFile(pending.ImageUrl);
            }

            pending.Status = "rejected";
            _context.PoiImagePendingChanges.Update(pending);
            
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var poiName = (await _context.Pois.FindAsync(pending.PoiId))?.Name ?? "N/A";
                await _logger.LogAsync(null, "Từ chối thay đổi ảnh", poiName, $"Admin đã từ chối yêu cầu {pending.ChangeType} cho {poiName}");
            }
            return result;
        }
        private void DeletePhysicalFile(string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_storagePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}
