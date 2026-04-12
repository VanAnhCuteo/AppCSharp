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

        // --- Search & Filter Handlers ---

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue;
            clearBtn.IsVisible = !string.IsNullOrEmpty(_searchText);
            
            // Immediate search for responsiveness
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

        private async void OnQuickFilterTapped(object sender, TappedEventArgs e)
        {
            var param = e.Parameter?.ToString() ?? "";
            
            if (param.StartsWith("TIME_"))
            {
                var val = int.Parse(param.Replace("TIME_", ""));
                if (val == 60) { _minDuration = 0; _maxDuration = 60; }
                else if (val == 120) { _minDuration = 61; _maxDuration = 120; }
                else if (val == 999) { _minDuration = 181; _maxDuration = 0; }
            }
            else if (param.StartsWith("PRICE_"))
            {
                var val = decimal.Parse(param.Replace("PRICE_", ""));
                _maxPrice = val;
            }

            // Visual feedback - optional: we could highlight the selected chip
            // For now, just reload
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