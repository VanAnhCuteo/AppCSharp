namespace FoodMapAPI.Models
{
    public class Food
    {
        public int id { get; set; }
        public int category_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string open_time { get; set; }
        public string image_url { get; set; }
        public int range_meters { get; set; } = 50;
    }

    public class AudioLogRequest
    {
        public int user_id { get; set; }
        public int duration_seconds { get; set; }
    }

    public class AudioStat
    {
        public int poi_id { get; set; }
        public string poi_name { get; set; }
        public int total_listens { get; set; }
        public double avg_duration { get; set; }
    }
}