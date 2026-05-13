namespace FoodMapAPI.Models
{
    public class Guide
    {
        public int guide_id { get; set; }
        public int poi_id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string language { get; set; }
        public int userId { get; set; }
    }
}
