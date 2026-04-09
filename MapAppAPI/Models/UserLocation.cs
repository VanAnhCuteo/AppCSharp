using System;

namespace FoodMapAPI.Models
{
    public class UserLocation
    {
        public int user_id { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public DateTime last_active { get; set; }
    }

    public class LocationUpdateRequest
    {
        public int user_id { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
}
