using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodMapAdmin.Models
{
    [Table("poi_qrs")]
    public class PoiQr
    {
        [Key]
        [Column("qr_id")]
        public int QrId { get; set; }

        [Column("poi_id")]
        public int PoiId { get; set; }

        [ForeignKey(nameof(PoiId))]
        public Poi? Poi { get; set; }

        [Required]
        [Column("qr_code_url")]
        public string QrCodeUrl { get; set; } = string.Empty;
    }
}
