using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodMapAdmin.Services
{
    public class TourService : ITourService
    {
        private readonly ApplicationDbContext _context;

        public TourService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _context.Tours.OrderByDescending(t => t.Id).ToListAsync();
        }

        public async Task<Tour?> GetTourByIdAsync(int id)
        {
            return await _context.Tours
                .Include(t => t.TourPois)
                .ThenInclude(tp => tp.Poi)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Tour> CreateTourAsync(Tour tour, List<TourPoi> tourPois)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();

            if (tourPois != null && tourPois.Any())
            {
                foreach (var tp in tourPois)
                {
                    tp.TourId = tour.Id;
                    tp.Poi = null; // Prevent EF tracking conflict
                }
                _context.TourPois.AddRange(tourPois);
                await _context.SaveChangesAsync();
            }

            return tour;
        }

        public async Task<Tour> UpdateTourAsync(Tour tour, List<TourPoi> tourPois)
        {
            _context.Tours.Update(tour);
            
            // Remove old pois
            var oldPois = await _context.TourPois.Where(tp => tp.TourId == tour.Id).ToListAsync();
            _context.TourPois.RemoveRange(oldPois);

            // Add new pois
            if (tourPois != null && tourPois.Any())
            {
                foreach (var tp in tourPois)
                {
                    tp.TourId = tour.Id;
                    tp.Id = 0; // Prevent identity insert overlap
                    tp.Poi = null; // Prevent EF tracking conflict
                }
                _context.TourPois.AddRange(tourPois);
            }

            await _context.SaveChangesAsync();
            return tour;
        }

        public async Task<bool> DeleteTourAsync(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return false;

            var oldPois = await _context.TourPois.Where(tp => tp.TourId == id).ToListAsync();
            _context.TourPois.RemoveRange(oldPois);
            
            var oldHistory = await _context.TourHistories.Where(th => th.TourId == id).ToListAsync();
            _context.TourHistories.RemoveRange(oldHistory);

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<TourPoi>> GetTourPoisAsync(int tourId)
        {
            return await _context.TourPois
                .Include(tp => tp.Poi)
                .Where(tp => tp.TourId == tourId)
                .OrderBy(tp => tp.OrderIndex)
                .ToListAsync();
        }

        public async Task<TourHistory> SaveTourHistoryAsync(int userId, int tourId, decimal progressPercentage, string status)
        {
            var history = await _context.TourHistories
                .FirstOrDefaultAsync(th => th.UserId == userId && th.TourId == tourId);
            
            if (history == null)
            {
                history = new TourHistory
                {
                    UserId = userId,
                    TourId = tourId,
                    ProgressPercentage = progressPercentage,
                    Status = status
                };
                _context.TourHistories.Add(history);
            }
            else
            {
                // Only update if progress is higher
                if (progressPercentage > history.ProgressPercentage)
                {
                    history.ProgressPercentage = progressPercentage;
                    history.Status = status;
                    history.CreatedAt = System.DateTime.UtcNow; // Update timestamp
                }
            }
            await _context.SaveChangesAsync();
            return history;
        }

        public async Task<List<TourHistory>> GetUserTourHistoryAsync(int userId)
        {
            return await _context.TourHistories
                .Include(th => th.Tour)
                .Where(th => th.UserId == userId)
                .OrderByDescending(th => th.CreatedAt)
                .ToListAsync();
        }
    }
}
