using System.Collections.Generic;

namespace FoodMapAPI.Models
{
    public class Tour
    {
        public int tour_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public int duration_minutes { get; set; }
        public decimal price { get; set; }
        public System.DateTime created_at { get; set; }

        // Navigation property for details (optional for basic listing)
        public List<TourPoiDetail>? pois { get; set; }
    }

    public class TourPoiDetail : Food
    {
        public int sequence_order { get; set; }
        public int stay_duration { get; set; }
        public decimal average_price { get; set; }
    }
}
