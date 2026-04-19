using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("is_hidden")]
        public bool IsHidden { get; set; } = false;

        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    }
}
