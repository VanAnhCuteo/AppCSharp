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
    }

    public class PoiImageService : IPoiImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IConfiguration _configuration;
        private readonly string _storagePath;

        public PoiImageService(ApplicationDbContext context, IActivityLogger logger, AuthenticationStateProvider authStateProvider, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _authStateProvider = authStateProvider;
            _configuration = configuration;
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
                .Include(pi => pi.Poi)
                .FirstOrDefaultAsync(pi => pi.ImageId == id);
        }

        public async Task<bool> CreateImageAsync(PoiImage image, Stream fileStream, string fileName)
        {
            try
            {
                // 1. Save file to storage path
                string filePath = Path.Combine(_storagePath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream);
                }

                // 2. Set image URL (standard format used in database)
                image.ImageUrl = $"/images/{fileName}";

                // 3. Save to database
                _context.PoiImages.Add(image);
                var result = await _context.SaveChangesAsync() > 0;

                if (result)
                {
                    var userId = await GetCurrentUserIdAsync();
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
                    var userId = await GetCurrentUserIdAsync();
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

                // Delete file
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
                    var userId = await GetCurrentUserIdAsync();
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
    }
}
