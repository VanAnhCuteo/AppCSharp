using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAPI.Models
{
    [Table("pois")]
    public class Poi
    {
        [Key]
        [Column("poi_id")]
        public int PoiId { get; set; }

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("address")]
        public string? Address { get; set; }

        [Column("latitude", TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column("longitude", TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [Column("range_meters")]
        public int RangeMeters { get; set; } = 50;

        [Column("is_hidden")]
        public bool IsHidden { get; set; } = false;

        public ICollection<PoiImage> Images { get; set; } = new List<PoiImage>();
    }
}
