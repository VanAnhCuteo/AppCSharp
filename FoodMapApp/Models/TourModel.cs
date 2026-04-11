using System;
using System.Collections.Generic;

namespace FoodMapApp.Models
{
    public class TourModel
    {
        public int tour_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public int duration_minutes { get; set; }
        public decimal price { get; set; }
        public DateTime created_at { get; set; }
        public List<TourPoiModel>? pois { get; set; }

        // Computed display properties
        public string DurationDisplay
        {
            get
            {
                if (duration_minutes >= 60)
                {
                    int hours = duration_minutes / 60;
                    int mins = duration_minutes % 60;
                    return mins > 0 ? $"{hours}h{mins}'" : $"{hours} giờ";
                }
                return $"{duration_minutes} phút";
            }
        }

        public string PriceDisplay
        {
            get
            {
                if (price >= 1000000)
                    return $"{price / 1000000:0.#}tr";
                if (price >= 1000)
                    return $"{price / 1000:0}k";
                return $"{price:0}đ";
            }
        }
    }

    public class TourPoiModel : FoodModel
    {
        public int sequence_order { get; set; }
        public int stay_duration { get; set; }
        public decimal average_price { get; set; }
    }
}
