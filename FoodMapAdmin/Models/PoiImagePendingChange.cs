using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_image_pending_changes")]
    public class PoiImagePendingChange
    {
        [Key]
        [Column("change_id")]
        public int ChangeId { get; set; }

        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Column("image_id")]
        public int? ImageId { get; set; }

        [ForeignKey(nameof(ImageId))]
        public PoiImage? OriginalImage { get; set; }

        [Column("image_url")]
        [StringLength(255)]
        public string? ImageUrl { get; set; }

        [Column("change_type")]
        [StringLength(20)]
        public string ChangeType { get; set; } = "add"; // add, update, delete

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
