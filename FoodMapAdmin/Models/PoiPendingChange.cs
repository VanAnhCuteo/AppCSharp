using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_pending_changes")]
    public class PoiPendingChange
    {
        [Key]
        [Column("change_id")]
        public int ChangeId { get; set; }

        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Column("change_type")]
        [StringLength(20)]
        public string ChangeType { get; set; } = "update"; // create, update, delete

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [Column("name")]
        [StringLength(200)]
        public string? Name { get; set; }

        [Column("address")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Column("latitude", TypeName = "decimal(10,7)")]
        public decimal? Latitude { get; set; }

        [Column("longitude", TypeName = "decimal(10,7)")]
        public decimal? Longitude { get; set; }

        [Column("open_time")]
        [StringLength(50)]
        public string? OpenTime { get; set; }

        [Column("range_meters")]
        public int RangeMeters { get; set; } = 50;

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? Requester { get; set; }

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
