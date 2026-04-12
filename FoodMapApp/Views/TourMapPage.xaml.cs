using System.Collections.Generic;
using System.Net.Http.Json;
using FoodMapApp.Models;

namespace FoodMapApp.Views
{
    public partial class TourMapPage : ContentPage
    {
        private int _tourId;
        private TourModel? _tour;
        private int _currentStopIndex = 0;
        private Location? _userLocation;
        private bool _isJourneyStarted = false;
        private int _visitedCount = 0;

        public TourMapPage(int tourId)
        {
            InitializeComponent();
            _tourId = tourId;

            tourMapView.Navigated += async (s, e) =>
            {
                if (e.Url.Contains("tour_map.html") && _tour != null)
                {
                    await SendTourToMap();
                }
            };
            tourMapView.Source = "tour_map.html";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTourDetails();
        }

        private async Task LoadTourDetails()
        {
            try
            {
                // Try to get current location
                try
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                    if (status == PermissionStatus.Granted)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        _userLocation = await Geolocation.Default.GetLocationAsync(request);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Location Error: {ex.Message}"); }

                using HttpClient client = new HttpClient();
                _tour = await client.GetFromJsonAsync<TourModel>($"{AppConfig.TourApiUrl}/{_tourId}");
                if (_tour != null)
                {
                    currentStopNameLabel.Text = _tour.name;
                    stopCountLabel.Text = $"{_tour.pois?.Count ?? 0} quán";
                    UpdateStopInfo();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể tải chi tiết tour. " + ex.Message, "OK");
            }
        }

        private async void UpdateStopInfo()
        {
            if (_tour?.pois == null || _tour.pois.Count == 0) return;

            if (!_isJourneyStarted)
            {
                statusTitleLabel.Text = "BẢN ĐỒ TOUR";
                currentStopNameLabel.Text = _tour.name;
                actionButton.Text = "Bắt đầu hành trình";
            }
            else
            {
                var currentPoi = _tour.pois[_currentStopIndex];
                statusTitleLabel.Text = $"ĐỊA ĐIỂM {_visitedCount}";
                currentStopNameLabel.Text = currentPoi.name;
                actionButton.Text = _visitedCount < _tour.pois.Count ? "Đến địa điểm tiếp theo" : "Hoàn thành hành trình";
                
                await tourMapView.EvaluateJavaScriptAsync($"focusStop({_currentStopIndex})");
            }

            await SendTourToMap();
        }

        private async Task SendTourToMap()
        {
            if (_tour?.pois == null) return;
            var json = System.Text.Json.JsonSerializer.Serialize(_tour.pois);
            string userLocParams = _userLocation != null 
                ? $"{_userLocation.Latitude}, {_userLocation.Longitude}" 
                : "null, null";
            
            // Pass the state: isJourneyStarted (0/1)
            int journeyState = _isJourneyStarted ? 1 : 0;
            await tourMapView.EvaluateJavaScriptAsync($"loadTourRoute({json}, {userLocParams}, {_currentStopIndex}, {journeyState})");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnNextStopClicked(object sender, EventArgs e)
        {
            if (_tour?.pois == null || _tour.pois.Count == 0) return;

            if (!_isJourneyStarted)
            {
                _currentStopIndex = FindNearestPoiIndex();
                _isJourneyStarted = true;
                _visitedCount = 1;
                UpdateStopInfo();
            }
            else
            {
                if (_visitedCount >= _tour.pois.Count)
                {
                    FinishTour();
                    return;
                }

                // Chuyển chặng theo vòng tròn (Phương án A)
                _currentStopIndex = (_currentStopIndex + 1) % _tour.pois.Count;
                _visitedCount++;
                UpdateStopInfo();
            }
        }

        private int FindNearestPoiIndex()
        {
            if (_tour?.pois == null || _userLocation == null) return 0;
            
            double minDistance = double.MaxValue;
            int nearestIndex = 0;

            for (int i = 0; i < _tour.pois.Count; i++)
            {
                var poi = _tour.pois[i];
                double dist = Location.CalculateDistance(_userLocation.Latitude, _userLocation.Longitude, poi.latitude, poi.longitude, DistanceUnits.Kilometers);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestIndex = i;
                }
            }
            return nearestIndex;
        }

        private async void FinishTour()
        {
            await DisplayAlert("Chúc mừng!", "Bạn đã hoàn thành toàn bộ hành trình tour.", "Tuyệt vời");
            await Navigation.PopAsync();
        }
    }
}
