using System;
using System.Collections.Generic;

namespace FoodMapAdmin.Models
{
    public class Tour
    {
        public int TourId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public decimal Price { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<TourPoi>? Pois { get; set; }
    }

    public class TourPoi
    {
        public int TourPoiId { get; set; }
        public int TourId { get; set; }
        public int PoiId { get; set; }
        public int SequenceOrder { get; set; }
        public int StayDuration { get; set; } = 30;
        public decimal AveragePrice { get; set; }

        // Navigation property for helper
        public Poi? Poi { get; set; }
    }
}
