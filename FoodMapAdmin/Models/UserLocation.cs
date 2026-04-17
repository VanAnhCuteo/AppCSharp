using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("user_locations")]
    public class UserLocation
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("latitude", TypeName = "decimal(10,7)")]
        public decimal Latitude { get; set; }

        [Column("longitude", TypeName = "decimal(10,7)")]
        public decimal Longitude { get; set; }

        [Column("last_active")]
        public DateTime LastActive { get; set; }

        [Column("is_listening_audio")]
        public bool IsListeningAudio { get; set; }

        [Column("listening_poi_id")]
        public int? ListeningPoiId { get; set; }
    }
}
