using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("pois")]
    public class Poi
    {
        [Key]
        [Column("poi_id")]
        public int PoiId { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Required]
        [StringLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [StringLength(255)]
        [Column("address")]
        public string? Address { get; set; }

        [Column("latitude", TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column("longitude", TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [StringLength(50)]
        [Column("open_time")]
        public string? OpenTime { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? Owner { get; set; }

        public ICollection<PoiImage> Images { get; set; } = new List<PoiImage>();
        public ICollection<PoiGuide> Guides { get; set; } = new List<PoiGuide>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
