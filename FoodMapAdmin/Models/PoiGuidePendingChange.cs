using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_guide_pending_changes")]
    public class PoiGuidePendingChange
    {
        [Key]
        [Column("change_id")]
        public int ChangeId { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Column("guide_id")]
        public int? GuideId { get; set; } // Null if it's a new guide

        [Column("change_type")]
        [StringLength(20)]
        public string ChangeType { get; set; } = "update"; // create, update, delete

        [Column("title")]
        [StringLength(200)]
        public string? Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("language")]
        [StringLength(20)]
        public string Language { get; set; } = "vi";

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? Requester { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, approved, rejected

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
