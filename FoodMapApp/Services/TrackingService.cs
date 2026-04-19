namespace FoodMapApp.Services
{
    public static class TrackingService
    {
        public static bool IsListening { get; set; } = false;
        public static int? CurrentPoiId { get; set; } = null;

        public static void UpdateStatus(bool isListening, int? poiId = null)
        {
            IsListening = isListening;
            if (poiId != null) CurrentPoiId = poiId;
        }

        public static void Stop()
        {
            IsListening = false;
        }
    }
}
