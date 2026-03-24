using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_images")]
    public class PoiImage
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [StringLength(255)]
        [Column("image_url")]
        public string? ImageUrl { get; set; }
    }
}
