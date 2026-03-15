namespace FoodMapAPI.Models
{
    public class Review
    {
        public int id { get; set; }
        public int poi_id { get; set; }
        public int user_id { get; set; }
        public int rating { get; set; }
        public string comment { get; set; }
        public string created_at { get; set; }
    }
}
