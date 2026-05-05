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
            // Only return tours that have at least one visible POI
            return await _context.Tours
                .Where(t => t.TourPois.Any(tp => !tp.Poi.IsHidden))
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<Tour?> GetTourByIdAsync(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.TourPois.Where(tp => !tp.Poi.IsHidden))
                    .ThenInclude(tp => tp.Poi)
                        .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            return tour;
        }
    }
}
