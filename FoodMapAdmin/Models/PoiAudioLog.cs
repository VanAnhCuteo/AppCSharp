using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_audio_logs")]
    public class PoiAudioLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("duration_seconds")]
        public int DurationSeconds { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("PoiId")]
        public virtual Poi? Poi { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    public class PoiAudioStat
    {
        public int PoiId { get; set; }
        public string? PoiName { get; set; }
        public int TotalListens { get; set; }
        public double AverageDurationSeconds { get; set; }
    }
}
