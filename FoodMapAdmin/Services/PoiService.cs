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

        public PoiService(ApplicationDbContext context, IActivityLogger logger, AuthenticationStateProvider authStateProvider, IPoiImageService imageService)
        {
            _context = context;
            _logger = logger;
            _authStateProvider = authStateProvider;
            _imageService = imageService;
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
            return await _context.Pois.Include(p => p.Category).Include(p => p.Owner).ToListAsync();
        }

        public async Task<List<Poi>> GetPoisByOwnerAsync(int userId)
        {
            return await _context.Pois
                .Where(p => p.UserId == userId)
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<Poi?> GetPoiByIdAsync(int id)
        {
            return await _context.Pois
                .AsNoTracking() // Quan trọng: Không track khi lấy dữ liệu để sửa, tránh auto-update
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.PoiId == id);
        }

        public async Task<bool> UpdatePoiAsync(Poi poi)
        {
            _context.Pois.Update(poi);
            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Cập nhật thông tin", poi.Name, $"Đã sửa dữ liệu của {poi.Name}");
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
            if (result)
            {
                var userId = await GetCurrentUserIdAsync();
                await _logger.LogAsync(userId, "Thêm quán ăn mới", poi.Name, $"Đã tạo mới {poi.Name}");
            }
            return result;
        }
    }
}
