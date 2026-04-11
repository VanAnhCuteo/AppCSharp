using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodMapAdmin.Data;
using FoodMapAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodMapAdmin.Services
{
    public interface ITourService
    {
        Task<List<Tour>> GetAllToursAsync();
        Task<Tour?> GetTourByIdAsync(int id);
        Task<int> CreateTourAsync(Tour tour);
        Task<bool> UpdateTourAsync(Tour tour);
        Task<bool> DeleteTourAsync(int id);
        Task<bool> AddPoiToTourAsync(int tourId, TourPoi detail);
        Task<bool> RemovePoiFromTourAsync(int tourId, int poiId);
        Task<bool> UpdateTourPoiAsync(TourPoi poi);
        Task<int> GetTotalPoiCountAsync();
    }

    public class TourService : ITourService
    {
        private readonly ApplicationDbContext _context;

        public TourService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tour>> GetAllToursAsync()
        {
            return await _context.Tours
                .Include(t => t.Pois!)
                    .ThenInclude(tp => tp.Poi)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Tour?> GetTourByIdAsync(int id)
        {
            return await _context.Tours
                .Include(t => t.Pois!)
                .ThenInclude(tp => tp.Poi)
                .FirstOrDefaultAsync(t => t.TourId == id);
        }

        public async Task<int> CreateTourAsync(Tour tour)
        {
            _context.Tours.Add(tour);
            await _context.SaveChangesAsync();
            return tour.TourId;
        }

        public async Task<bool> UpdateTourAsync(Tour tour)
        {
            var dbTour = await _context.Tours.FindAsync(tour.TourId);
            if (dbTour == null) return false;

            dbTour.Name = tour.Name;
            dbTour.Description = tour.Description;
            dbTour.DurationMinutes = tour.DurationMinutes;
            dbTour.Price = tour.Price;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteTourAsync(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return false;

            _context.Tours.Remove(tour);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddPoiToTourAsync(int tourId, TourPoi detail)
        {
            detail.TourId = tourId;
            _context.TourPois.Add(detail);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemovePoiFromTourAsync(int tourId, int poiId)
        {
            var detail = await _context.TourPois
                .FirstOrDefaultAsync(tp => tp.TourId == tourId && tp.PoiId == poiId);
            
            if (detail == null) return false;

            _context.TourPois.Remove(detail);

            // Re-sequence remaining items
            var remaining = await _context.TourPois
                .Where(tp => tp.TourId == tourId && tp.PoiId != poiId)
                .OrderBy(tp => tp.SequenceOrder)
                .ToListAsync();

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].SequenceOrder = i + 1;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateTourPoiAsync(TourPoi poi)
        {
            var dbPoi = await _context.TourPois.FindAsync(poi.TourPoiId);
            if (dbPoi == null) return false;

            dbPoi.StayDuration = poi.StayDuration;
            dbPoi.AveragePrice = poi.AveragePrice;
            dbPoi.SequenceOrder = poi.SequenceOrder;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> GetTotalPoiCountAsync()
        {
            return await _context.TourPois.CountAsync();
        }
    }
}
