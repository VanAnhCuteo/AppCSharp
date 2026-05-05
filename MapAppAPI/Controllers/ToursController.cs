using FoodMapAPI.Models;
using FoodMapAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodMapAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ToursController : ControllerBase
    {
        private readonly ITourService _tourService;

        public ToursController(ITourService tourService)
        {
            _tourService = tourService;
        }

        [HttpGet]
        public async Task<ActionResult> GetAllTours()
        {
            try
            {
                var tours = await _tourService.GetAllToursAsync();
                return Ok(tours);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Internal Error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTourDetails(int id)
        {
            var tour = await _tourService.GetTourByIdAsync(id);
            if (tour == null) return NotFound();

            var pois = tour.TourPois.OrderBy(tp => tp.OrderIndex).Select(tp => new
            {
                Id = tp.Id,
                TourId = tp.TourId,
                PoiId = tp.PoiId,
                OrderIndex = tp.OrderIndex,
                StayDurationMinutes = tp.StayDurationMinutes,
                ApproximatePrice = tp.ApproximatePrice,
                Poi = new
                {
                    Id = tp.Poi?.PoiId,
                    Name = tp.Poi?.Name,
                    Address = tp.Poi?.Address,
                    Latitude = tp.Poi?.Latitude,
                    Longitude = tp.Poi?.Longitude,
                    Images = tp.Poi?.Images.Select(i => i.ImageUrl).ToList(),
                    RangeMeters = tp.Poi?.RangeMeters ?? 50
                }
            });

            return Ok(new
            {
                tour.Id,
                tour.Name,
                tour.Description,
                tour.CreatedAt,
                TourPois = pois
            });
        }
    }
}
