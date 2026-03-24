using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        [Column("review_id")]
        public int ReviewId { get; set; }

        [Column("poi_id")]
        public int? PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
