using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("languages")]
    public class Language
    {
        [Key]
        [Column("language_code")]
        public string LanguageCode { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("flag_url")]
        public string? FlagUrl { get; set; }
    }
}
