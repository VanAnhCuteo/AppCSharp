using System.Diagnostics;
using System.Text.Json;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage
    {
        private Location? _lastGpsLocation;
        private IDispatcherTimer? _locationTimer;

        private void StartLocationTracking()
        {
            _locationTimer = Dispatcher.CreateTimer();
            _locationTimer.Interval = TimeSpan.FromSeconds(4);
            _locationTimer.Tick += async (s, e) => await ReportCurrentLocationAsync();
            _locationTimer.Start();
            
            _ = ReportCurrentLocationAsync();
        }

        private async Task ReportCurrentLocationAsync()
        {
            try
            {
                int userId = Preferences.Default.Get("user_id", 0);
                if (userId == 0) return;

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    if (_lastGpsLocation != null && _locationTimer != null)
                    {
                        double dist = _lastGpsLocation.CalculateDistance(location, DistanceUnits.Kilometers) * 1000;
                        if (dist < 10) 
                            _locationTimer.Interval = TimeSpan.FromSeconds(10); 
                        else
                            _locationTimer.Interval = TimeSpan.FromSeconds(4);
                    }
                    _lastGpsLocation = location;

                    if (_foodsJson != null)
                    {
                        var foods = JsonSerializer.Deserialize<List<FoodModel>>(_foodsJson);
                        if (foods != null)
                            AutoAudioService.Instance.UpdateQueue(location, foods, LocalizationService.Instance.CurrentLanguage);
                    }

                    bool isListening = TrackingService.IsListening;
                    var session = _activeSession ?? _manualSession;
                    if (session != null && session.IsPaused) isListening = false;

                    int? currentPoiId = TrackingService.CurrentPoiId;

                    var payload = new { 
                        user_id = userId, 
                        latitude = location.Latitude, 
                        longitude = location.Longitude,
                        is_listening = isListening,
                        poi_id = currentPoiId
                    };

                    using HttpClient client = new HttpClient();
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    await client.PostAsync($"{AppConfig.AuthApiUrl}/update-location", content);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"DEBUG: Error reporting location: {ex.Message}"); }
        }
    }
}
