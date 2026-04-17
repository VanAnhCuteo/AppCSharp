using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAPI.Models
{
    public class TourPoi
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("tour_id")]
        public int TourId { get; set; }
        
        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }
        
        [ForeignKey("PoiId")]
        public Poi? Poi { get; set; }

        [Column("stay_duration_minutes")]
        public int StayDurationMinutes { get; set; } = 30;

        [Column("approximate_price")]
        public string? ApproximatePrice { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; }
    }
}
