using FoodMapAPI.Data;
using FoodMapAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodMapAPI.Services
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
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<TourHistory> SaveTourHistoryAsync(int userId, int tourId, decimal progressPercentage, string status)
        {
            var existing = await _context.TourHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.TourId == tourId);

            if (existing != null)
            {
                // Only update if progress is better
                if (progressPercentage > existing.ProgressPercentage)
                {
                    existing.ProgressPercentage = progressPercentage;
                    existing.Status = status;
                    existing.CreatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return existing;
            }
            else
            {
                var history = new TourHistory
                {
                    UserId = userId,
                    TourId = tourId,
                    ProgressPercentage = progressPercentage,
                    Status = status,
                    CreatedAt = DateTime.Now
                };
                _context.TourHistories.Add(history);
                await _context.SaveChangesAsync();
                return history;
            }
        }

        public async Task<List<TourHistory>> GetUserTourHistoryAsync(int userId)
        {
            return await _context.TourHistories
                .Include(h => h.Tour)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}
