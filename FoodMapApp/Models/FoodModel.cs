using System.Text.Json.Serialization;

namespace FoodMapApp.Models
{
    public class FoodModel
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
        public int range_meters { get; set; }

        [JsonPropertyName("qr_code_url")]
        public string qr_code_url { get; set; }
    }
}
