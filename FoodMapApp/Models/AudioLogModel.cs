using System;

namespace FoodMapApp.Models
{
    public class AudioLogModel
    {
        public int log_id { get; set; }
        public int poi_id { get; set; }
        public string poi_name { get; set; } = string.Empty;
        public string? poi_image_url { get; set; }
        public int duration_seconds { get; set; }
        public DateTime created_at { get; set; }

        public string DisplayDate => created_at.ToString("HH:mm - dd/MM/yyyy");
        public string DisplayDuration => $"{duration_seconds}s";
    }
}
