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
    }
}