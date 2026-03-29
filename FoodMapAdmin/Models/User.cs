using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [StringLength(255)]
        [Column("password")]
        public string? Password { get; set; }

        [Column("role")]
        public string Role { get; set; } = "user"; // 'user', 'admin', 'CNH'

        [Column("is_blocked")]
        public bool IsBlocked { get; set; } = false;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    }
}
