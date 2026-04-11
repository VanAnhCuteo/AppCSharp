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
                using HttpClient client = new HttpClient();
                _tour = await client.GetFromJsonAsync<TourModel>($"{AppConfig.TourApiUrl}/{_tourId}");
                if (_tour != null)
                {
                    tourTitleLabel.Text = _tour.name;
                    tourSubtitleLabel.Text = $"{_tour.DurationDisplay} • {_tour.PriceDisplay}";
                    UpdateStopInfo();
                    await SendTourToMap();
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

            if (_currentStopIndex >= _tour.pois.Count)
            {
                await DisplayAlert("Hoàn thành", "Bạn đã hoàn thành tour này!", "Tuyệt vời");
                await Navigation.PopAsync();
                return;
            }

            var poi = _tour.pois[_currentStopIndex];
            stopNumberLabel.Text = (_currentStopIndex + 1).ToString();
            stopNameLabel.Text = poi.name;
            stopDurationLabel.Text = $"{poi.stay_duration} phút";
            stopPriceLabel.Text = $"{poi.average_price:N0} VNĐ";

            // Tell the map to focus on this stop
            await tourMapView.EvaluateJavaScriptAsync($"focusStop({_currentStopIndex})");
        }

        private async Task SendTourToMap()
        {
            if (_tour?.pois == null) return;
            var json = System.Text.Json.JsonSerializer.Serialize(_tour.pois);
            await tourMapView.EvaluateJavaScriptAsync($"loadTourRoute({json})");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnNextStopClicked(object sender, EventArgs e)
        {
            _currentStopIndex++;
            UpdateStopInfo();
        }
    }
}
