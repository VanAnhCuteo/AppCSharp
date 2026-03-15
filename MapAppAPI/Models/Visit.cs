namespace FoodMapAPI.Models
{
    public class Visit
    {
        public int visit_id { get; set; }
        public int poi_id { get; set; }
        public int user_id { get; set; }
        public string visit_time { get; set; }
    }
}
