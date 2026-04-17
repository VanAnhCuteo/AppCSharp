using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("tour_histories")]
    public class TourHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("tour_id")]
        public int TourId { get; set; }

        // "Completed 50%" or "Completed 100%" or "InProgress"
        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "InProgress";

        [Column("progress_percentage")]
        public decimal ProgressPercentage { get; set; } = 0;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(TourId))]
        public Tour? Tour { get; set; }
    }
}
