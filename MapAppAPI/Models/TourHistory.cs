using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAPI.Models
{
    public class TourHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }
        
        [Column("tour_id")]
        public int TourId { get; set; }
        
        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        [Column("status")]
        public string Status { get; set; } = "InProgress";

        [Column("progress_percentage")]
        public decimal ProgressPercentage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
