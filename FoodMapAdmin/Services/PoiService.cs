using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace FoodMapAdmin.Services
{
    public interface IPoiService
    {
        Task<List<Poi>> GetAllPoisAsync();
        Task<List<Poi>> GetPoisByOwnerAsync(int userId);
        Task<Poi?> GetPoiByIdAsync(int id);
        Task<bool> UpdatePoiAsync(Poi poi);
        Task<bool> DeletePoiAsync(int id);
        Task<bool> CreatePoiAsync(Poi poi);
    }

    public class PoiService : IPoiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IPoiImageService _imageService;
        private readonly IConfiguration _configuration;
        private readonly string _storagePath;

        public PoiService(ApplicationDbContext context, IActivityLogger logger, AuthenticationStateProvider authStateProvider, IPoiImageService imageService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _authStateProvider = authStateProvider;
            _imageService = imageService;
            _configuration = configuration;
            _storagePath = _configuration["ImageStoragePath"] ?? @"d:\MapApp\FoodMapApp\Resources\Raw\images";
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userIdStr = user.FindFirst(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var id) ? id : null;
        }

        public async Task<List<Poi>> GetAllPoisAsync()
        {
            return await _context.Pois
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .Include(p => p.QrCode)
                .ToListAsync();
        }

        public async Task<List<Poi>> GetPoisByOwnerAsync(int userId)
        {
            return await _context.Pois
                .Where(p => p.UserId == userId)
                .Include(p => p.Category)
                .Include(p => p.QrCode)
                .ToListAsync();
        }

        public async Task<Poi?> GetPoiByIdAsync(int id)
        {
            return await _context.Pois
                .AsNoTracking() // Quan trọng: Không track khi lấy dữ liệu để sửa, tránh auto-update
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.QrCode)
                .FirstOrDefaultAsync(p => p.PoiId == id);
        }

        public async Task<bool> UpdatePoiAsync(Poi poi)
        {
            // Lấy quán ăn gốc từ database để cập nhật (giúp tránh lỗi tracking)
            var dbPoi = await _context.Pois.FirstOrDefaultAsync(p => p.PoiId == poi.PoiId);
            if (dbPoi == null) return false;

            // Thu công cập nhật các trường quan trọng
            dbPoi.Name = poi.Name;
            dbPoi.Address = poi.Address;
            dbPoi.CategoryId = poi.CategoryId;
            dbPoi.RangeMeters = poi.RangeMeters;
            dbPoi.Latitude = poi.Latitude;
            dbPoi.Longitude = poi.Longitude;
            dbPoi.OpenTime = poi.OpenTime;
            dbPoi.UserId = poi.UserId;

            // Xử lý thông minh cho QR Code (Tự động dọn dẹp tệp cũ)
            var existingQr = await _context.PoiQrs.FirstOrDefaultAsync(q => q.PoiId == poi.PoiId);
            
            if (string.IsNullOrEmpty(poi.QrCodeUrl))
            {
                // Hành động: Xóa QR hiện tại
                if (existingQr != null)
                {
                    DeletePhysicalQrFile(existingQr.QrCodeUrl);
                    _context.PoiQrs.Remove(existingQr);
                }
            }
            else
            {
                // Hành động: Thêm mới hoặc Cập nhật QR
                if (existingQr == null)
                {
                    _context.PoiQrs.Add(new PoiQr { PoiId = poi.PoiId, QrCodeUrl = poi.QrCodeUrl });
                }
                else if (existingQr.QrCodeUrl != poi.QrCodeUrl)
                {
                    // Nếu URL thay đổi (có ảnh mới), xóa tệp cũ
                    DeletePhysicalQrFile(existingQr.QrCodeUrl);
                    existingQr.QrCodeUrl = poi.QrCodeUrl;
                }
            }

            // Ghi nhật ký và lưu thay đổi
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Cập nhật thông tin", dbPoi.Name, $"Đã sửa dữ liệu của {dbPoi.Name}");
            }
            return result;
        }

        public async Task<bool> DeletePoiAsync(int id)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return false;
            
            var name = poi.Name;

            // Xóa ảnh của quán và tệp vật lý trước khi xóa quán
            await _imageService.DeleteImagesByPoiIdAsync(id);

            // Xóa mã QR liên quan và tệp vật lý
            var qr = await _context.PoiQrs.FirstOrDefaultAsync(q => q.PoiId == id);
            if (qr != null) 
            {
                DeletePhysicalQrFile(qr.QrCodeUrl);
                _context.PoiQrs.Remove(qr);
            }

            _context.Pois.Remove(poi);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Xóa quán ăn", name, $"Đã xóa {name} khỏi hệ thống");
            }
            return result;
        }

        public async Task<bool> CreatePoiAsync(Poi poi)
        {
            _context.Pois.Add(poi);
            var result = await _context.SaveChangesAsync() > 0;
            
            // Save QR Code if exists after ID is generated
            if (result && !string.IsNullOrEmpty(poi.QrCodeUrl))
            {
                _context.PoiQrs.Add(new PoiQr { PoiId = poi.PoiId, QrCodeUrl = poi.QrCodeUrl });
                await _context.SaveChangesAsync();
            }

            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Thêm quán ăn mới", poi.Name, $"Đã tạo mới {poi.Name}");
            }
            return result;
        }
        private void DeletePhysicalQrFile(string? qrUrl)
        {
            if (string.IsNullOrEmpty(qrUrl)) return;
            try
            {
                var fileName = Path.GetFileName(qrUrl);
                var filePath = Path.Combine(_storagePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting QR file: {ex.Message}");
            }
        }
    }
}
