using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_visits")]
    public class PoiVisit
    {
        [Key]
        [Column("visit_id")]
        public int VisitId { get; set; }

        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Column("visit_time")]
        public DateTime? VisitTime { get; set; }

        [Column("poi_name")]
        [StringLength(200)]
        public string? PoiName { get; set; }

        [Column("poi_address")]
        [StringLength(255)]
        public string? PoiAddress { get; set; }
    }
}
