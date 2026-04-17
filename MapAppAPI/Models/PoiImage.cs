using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAPI.Models
{
    [Table("poi_images")]
    public class PoiImage
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }

        [Column("image_url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
