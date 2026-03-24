using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

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

        public PoiService(ApplicationDbContext context)
        {
            _context = context;
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
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.PoiId == id);
        }

        public async Task<bool> UpdatePoiAsync(Poi poi)
        {
            _context.Pois.Update(poi);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeletePoiAsync(int id)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return false;
            _context.Pois.Remove(poi);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> CreatePoiAsync(Poi poi)
        {
            _context.Pois.Add(poi);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
