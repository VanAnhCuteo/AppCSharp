using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("tours")]
    public class Tour
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<TourPoi> TourPois { get; set; } = new List<TourPoi>();
        public ICollection<TourHistory> TourHistories { get; set; } = new List<TourHistory>();
    }
}
