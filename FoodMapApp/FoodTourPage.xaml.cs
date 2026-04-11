using System.Collections.ObjectModel;
using System.Net.Http.Json;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class FoodTourPage : ContentPage
    {
        private int _maxDuration = 0;
        private int _minDuration = 0;
        private decimal _maxPrice = 0;
        private string _searchText = string.Empty;

        public FoodTourPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTours();
        }

        private async void OnRefresh(object sender, EventArgs e)
        {
            await LoadTours();
            refreshView.IsRefreshing = false;
        }

        private async Task LoadTours()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string url = AppConfig.TourApiUrl;
                var queryParams = new List<string>();

                if (!string.IsNullOrEmpty(_searchText))
                    queryParams.Add($"search={Uri.EscapeDataString(_searchText)}");
                if (_minDuration > 0)
                    queryParams.Add($"min_duration={_minDuration}");
                if (_maxDuration > 0 && _maxDuration < 999)
                    queryParams.Add($"max_duration={_maxDuration}");
                if (_maxPrice > 0 && _maxPrice < 999999)
                    queryParams.Add($"max_price={_maxPrice}");

                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                var tours = await client.GetFromJsonAsync<List<TourModel>>(url);
                tourList.ItemsSource = new ObservableCollection<TourModel>(tours ?? new List<TourModel>());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", "Không thể tải danh sách tour. " + ex.Message, "OK");
            }
        }

        // --- Search & Suggestion Handlers ---

        private async void OnSearchFocused(object sender, FocusEventArgs e)
        {
            suggestionOverlay.IsVisible = true;
            await Task.WhenAll(
                suggestionOverlay.FadeTo(1, 250),
                suggestionPanel.TranslateTo(0, 0, 250, Easing.CubicOut)
            );
        }

        private async void OnSearchUnfocused(object sender, FocusEventArgs e)
        {
            // Note: Don't hide immediately to allow suggestion taps to register
        }

        private async void OnOverlayTapped(object sender, EventArgs e)
        {
            await HideSuggestions();
            searchEntry.Unfocus();
        }

        private async Task HideSuggestions()
        {
            await Task.WhenAll(
                suggestionOverlay.FadeTo(0, 200),
                suggestionPanel.TranslateTo(0, -20, 200, Easing.CubicIn)
            );
            suggestionOverlay.IsVisible = false;
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue;
            clearBtn.IsVisible = !string.IsNullOrEmpty(_searchText);
            
            // Debounce or immediate search? Immediate for small data
            await LoadTours();
        }

        private async void OnClearSearchClicked(object sender, EventArgs e)
        {
            searchEntry.Text = string.Empty;
            _searchText = string.Empty;
            _minDuration = 0;
            _maxDuration = 0;
            _maxPrice = 0;
            await LoadTours();
        }

        private async void OnSuggestionTapped(object sender, TappedEventArgs e)
        {
            var param = e.Parameter?.ToString() ?? "";
            
            if (param.StartsWith("TIME_"))
            {
                var val = int.Parse(param.Replace("TIME_", ""));
                if (val == 60) { _minDuration = 0; _maxDuration = 60; searchEntry.Text = "Dưới 1 giờ"; }
                else if (val == 120) { _minDuration = 61; _maxDuration = 120; searchEntry.Text = "1 - 2 giờ"; }
                else if (val == 999) { _minDuration = 181; _maxDuration = 0; searchEntry.Text = "Trên 3 giờ"; }
            }
            else if (param.StartsWith("PRICE_"))
            {
                var val = decimal.Parse(param.Replace("PRICE_", ""));
                _maxPrice = val;
                if (val == 100000) searchEntry.Text = "Tour Tiết kiệm";
                else if (val == 300000) searchEntry.Text = "Tour Trung bình";
                else if (val == 999999) searchEntry.Text = "Tour Cao cấp";
            }

            await HideSuggestions();
            searchEntry.Unfocus();
            await LoadTours();
        }

        private async void OnTourSelected(object sender, TappedEventArgs e)
        {
            if (e.Parameter is TourModel tour)
            {
                await Navigation.PushAsync(new Views.TourMapPage(tour.tour_id));
            }
        }
    }
}