using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("tour_pois")]
    public class TourPoi
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("tour_id")]
        public int TourId { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }

        [Column("stay_duration_minutes")]
        public int StayDurationMinutes { get; set; } = 30;

        [StringLength(100)]
        [Column("approximate_price")]
        public string? ApproximatePrice { get; set; }

        [Column("order_index")]
        public int OrderIndex { get; set; } = 0;

        [ForeignKey(nameof(TourId))]
        public Tour? Tour { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }
    }
}
