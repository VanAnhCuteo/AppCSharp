using System;

namespace FoodMapAPI.Models
{
    public class UserLocation
    {
        public int user_id { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public DateTime last_active { get; set; }
        public bool is_listening_audio { get; set; }
        public int? listening_poi_id { get; set; }
    }

    public class LocationUpdateRequest
    {
        public int user_id { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public bool is_listening { get; set; }
        public int? poi_id { get; set; }
    }
}
