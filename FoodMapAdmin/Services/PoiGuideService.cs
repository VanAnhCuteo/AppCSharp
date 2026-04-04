using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface IPoiGuideService
    {
        Task<List<PoiGuide>> GetAllGuidesAsync();
        Task<PoiGuide?> GetGuideByIdAsync(int id);
        Task<List<PoiGuide>> GetGuidesByPoiIdAsync(int poiId);
        Task<PoiGuide?> GetGuideByPoiIdAsync(int poiId);
        Task<bool> UpdateGuideAsync(PoiGuide guide);
        Task<bool> CreateGuideAsync(PoiGuide guide);
        Task<bool> DeleteGuideAsync(int id);
    }

    public class PoiGuideService : IPoiGuideService
    {
        private readonly ApplicationDbContext _context;

        public PoiGuideService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PoiGuide>> GetAllGuidesAsync()
        {
            return await _context.PoiGuides
                .AsNoTracking()
                .Include(g => g.Poi)
                .OrderByDescending(g => g.GuideId)
                .ToListAsync();
        }

        public async Task<PoiGuide?> GetGuideByIdAsync(int id)
        {
            return await _context.PoiGuides
                .AsNoTracking()
                .Include(g => g.Poi)
                .FirstOrDefaultAsync(g => g.GuideId == id);
        }

        public async Task<PoiGuide?> GetGuideByPoiIdAsync(int poiId)
        {
            return await _context.PoiGuides
                .AsNoTracking()
                .Include(g => g.Poi)
                .FirstOrDefaultAsync(g => g.PoiId == poiId);
        }

        public async Task<List<PoiGuide>> GetGuidesByPoiIdAsync(int poiId)
        {
            return await _context.PoiGuides
                .AsNoTracking()
                .Include(g => g.Poi)
                .Where(g => g.PoiId == poiId)
                .ToListAsync();
        }

        public async Task<bool> UpdateGuideAsync(PoiGuide guide)
        {
            // Nullify navigation property to avoid tracking conflicts (Identity Map conflict)
            var poi = guide.Poi;
            guide.Poi = null;
            _context.PoiGuides.Update(guide);
            var result = await _context.SaveChangesAsync() > 0;
            guide.Poi = poi;
            return result;
        }

        public async Task<bool> CreateGuideAsync(PoiGuide guide)
        {
            var poi = guide.Poi;
            guide.Poi = null;
            _context.PoiGuides.Add(guide);
            var result = await _context.SaveChangesAsync() > 0;
            guide.Poi = poi;
            return result;
        }

        public async Task<bool> DeleteGuideAsync(int id)
        {
            var guide = await _context.PoiGuides.FindAsync(id);
            if (guide == null) return false;
            _context.PoiGuides.Remove(guide);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
