using FoodMapAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodMapAdmin.Services
{
    public interface ITourService
    {
        Task<List<Tour>> GetAllToursAsync();
        Task<Tour?> GetTourByIdAsync(int id);
        Task<Tour> CreateTourAsync(Tour tour, List<TourPoi> tourPois);
        Task<Tour> UpdateTourAsync(Tour tour, List<TourPoi> tourPois);
        Task<bool> DeleteTourAsync(int id);
        Task<List<TourPoi>> GetTourPoisAsync(int tourId);
        
        // History
        Task<TourHistory> SaveTourHistoryAsync(int userId, int tourId, decimal progressPercentage, string status);
        Task<List<TourHistory>> GetUserTourHistoryAsync(int userId);
    }
}
