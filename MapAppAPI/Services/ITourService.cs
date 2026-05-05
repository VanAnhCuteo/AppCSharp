using FoodMapAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoodMapAPI.Services
{
    public interface ITourService
    {
        Task<List<Tour>> GetAllToursAsync();
        Task<Tour?> GetTourByIdAsync(int id);
    }
}
