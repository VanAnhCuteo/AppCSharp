using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_guides")]
    public class PoiGuide
    {
        [Key]
        [Column("guide_id")]
        public int GuideId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn quán ăn")]
        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [StringLength(200)]
        [Column("title")]
        public string? Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [StringLength(20)]
        [Column("language")]
        public string? Language { get; set; }
    }
}
