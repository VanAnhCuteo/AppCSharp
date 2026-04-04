using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace FoodMapAdmin.Services
{
    public interface IPoiImageService
    {
        Task<List<PoiImage>> GetAllImagesAsync();
        Task<List<PoiImage>> GetImagesByOwnerAsync(int userId);
        Task<PoiImage?> GetImageByIdAsync(int id);
        Task<bool> CreateImageAsync(PoiImage image, Stream fileStream, string fileName);
        Task<bool> UpdateImageAsync(PoiImage image, Stream? fileStream, string? fileName);
        Task<bool> DeleteImageAsync(int id);
        Task<bool> DeleteImagesByPoiIdAsync(int poiId);
    }

    public class PoiImageService : IPoiImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IConfiguration _configuration;
        private readonly IPoiImagePendingChangeService _pendingService;
        private readonly string _storagePath;

        public PoiImageService(ApplicationDbContext context, IActivityLogger logger, AuthenticationStateProvider authStateProvider, IConfiguration configuration, IPoiImagePendingChangeService pendingService)
        {
            _context = context;
            _logger = logger;
            _authStateProvider = authStateProvider;
            _configuration = configuration;
            _pendingService = pendingService;
            _storagePath = _configuration["ImageStoragePath"] ?? @"d:\MapApp\FoodMapApp\Resources\Raw\images";
            
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userIdStr = user.FindFirst(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var id) ? id : null;
        }

        public async Task<List<PoiImage>> GetAllImagesAsync()
        {
            return await _context.PoiImages
                .Include(pi => pi.Poi)
                .Where(pi => pi.PoiId != null) // Bỏ mục chưa có (orphan images)
                .ToListAsync();
        }

        public async Task<List<PoiImage>> GetImagesByOwnerAsync(int userId)
        {
            return await _context.PoiImages
                .Include(pi => pi.Poi)
                .Where(pi => pi.Poi != null && pi.Poi.UserId == userId)
                .ToListAsync();
        }

        public async Task<PoiImage?> GetImageByIdAsync(int id)
        {
            return await _context.PoiImages
                .AsNoTracking() // Không track khi lấy dữ liệu để tránh auto-update nhầm
                .Include(pi => pi.Poi)
                .FirstOrDefaultAsync(pi => pi.ImageId == id);
        }

        public async Task<bool> CreateImageAsync(PoiImage image, Stream fileStream, string fileName)
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var userId = await GetCurrentUserIdAsync();

                // 1. Save file to storage path
                string filePath = Path.Combine(_storagePath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream);
                }

                string imageUrl = $"/images/{fileName}";

                if (!user.IsInRole("admin"))
                {
                    // Create pending request for CNH
                    var pending = new PoiImagePendingChange
                    {
                        PoiId = image.PoiId,
                        ImageUrl = imageUrl,
                        ChangeType = "add",
                        UserId = userId,
                        Status = "pending",
                        CreatedAt = DateTime.Now
                    };
                    return await _pendingService.CreatePendingChangeAsync(pending);
                }

                // Admin flow: Save directly to database
                image.ImageUrl = imageUrl;
                _context.PoiImages.Add(image);
                var result = await _context.SaveChangesAsync() > 0;

                if (result)
                {
                    var poiName = (await _context.Pois.FindAsync(image.PoiId))?.Name ?? "N/A";
                    await _logger.LogAsync(userId, "Thêm ảnh mới", poiName, $"Đã tải lên ảnh {fileName} cho {poiName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating image: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateImageAsync(PoiImage image, Stream? fileStream, string? fileName)
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var userId = await GetCurrentUserIdAsync();

                if (!user.IsInRole("admin"))
                {
                    // CNH updating an image metadata or file
                    var pending = new PoiImagePendingChange
                    {
                        PoiId = image.PoiId,
                        ImageId = image.ImageId, // The one being replaced
                        ChangeType = "update",
                        UserId = userId,
                        Status = "pending",
                        CreatedAt = DateTime.Now
                    };

                    if (fileStream != null && !string.IsNullOrEmpty(fileName))
                    {
                        // Save new file
                        string filePath = Path.Combine(_storagePath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileStream.CopyToAsync(stream);
                        }
                        pending.ImageUrl = $"/images/{fileName}";
                    }
                    else
                    {
                        // Use existing ImageUrl for metadata-only update
                        pending.ImageUrl = image.ImageUrl;
                    }

                    return await _pendingService.CreatePendingChangeAsync(pending);
                }

                // Admin flow
                if (fileStream != null && !string.IsNullOrEmpty(fileName))
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(image.ImageUrl))
                    {
                        var oldFileName = Path.GetFileName(image.ImageUrl);
                        var oldPath = Path.Combine(_storagePath, oldFileName);
                        if (File.Exists(oldPath))
                        {
                            File.Delete(oldPath);
                        }
                    }

                    // Save new file
                    string filePath = Path.Combine(_storagePath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileStream.CopyToAsync(stream);
                    }
                    image.ImageUrl = $"/images/{fileName}";
                }

                _context.PoiImages.Update(image);
                var result = await _context.SaveChangesAsync() > 0;

                if (result)
                {
                    var poiName = (await _context.Pois.FindAsync(image.PoiId))?.Name ?? "N/A";
                    await _logger.LogAsync(userId, "Cập nhật ảnh", poiName, $"Đã sửa thông tin ảnh của {poiName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating image: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            try
            {
                var image = await _context.PoiImages.FindAsync(id);
                if (image == null) return false;

                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var userId = await GetCurrentUserIdAsync();

                if (!user.IsInRole("admin"))
                {
                    var pending = new PoiImagePendingChange
                    {
                        PoiId = image.PoiId,
                        ImageId = image.ImageId,
                        ImageUrl = image.ImageUrl,
                        ChangeType = "delete",
                        UserId = userId,
                        Status = "pending",
                        CreatedAt = DateTime.Now
                    };
                    return await _pendingService.CreatePendingChangeAsync(pending);
                }

                // Admin flow: Delete file and record
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var fileName = Path.GetFileName(image.ImageUrl);
                    var filePath = Path.Combine(_storagePath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                var poiName = (await _context.Pois.FindAsync(image.PoiId))?.Name ?? "N/A";
                _context.PoiImages.Remove(image);
                var result = await _context.SaveChangesAsync() > 0;

                if (result)
                {
                    await _logger.LogAsync(userId, "Xóa ảnh", poiName, $"Đã xóa một ảnh của {poiName}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image: {ex.Message}");
                return false;
            }
        }
        public async Task<bool> DeleteImagesByPoiIdAsync(int poiId)
        {
            try
            {
                var images = await _context.PoiImages.Where(pi => pi.PoiId == poiId).ToListAsync();
                if (!images.Any()) return true;

                foreach (var img in images)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var fileName = Path.GetFileName(img.ImageUrl);
                        var filePath = Path.Combine(_storagePath, fileName);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }

                _context.PoiImages.RemoveRange(images);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting images for POI {poiId}: {ex.Message}");
                return false;
            }
        }
    }
}
