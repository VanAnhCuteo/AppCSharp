namespace FoodMapApp.Models
{
    public class TourModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TourDetailModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TourPoiModel> TourPois { get; set; } = new();
    }

    public class TourPoiModel
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public int StayDurationMinutes { get; set; }
        public string ApproximatePrice { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string PoiName { get; set; } = string.Empty;
        public FoodModel? Poi { get; set; }
    }
}
