namespace FoodMapApp.Models
{
    public class TourHistoryModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TourId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string TourName { get; set; } = string.Empty;
    }
}
