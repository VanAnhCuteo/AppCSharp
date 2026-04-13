using System.Collections.ObjectModel;
using System.Net.Http.Json;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class FoodTourPage : ContentPage
    {
        private int _minDuration = 0;
        private int _maxDuration = 0;
        private decimal _minPrice = 0;
        private decimal _maxPrice = 0;
        private string _searchText = string.Empty;
        private CancellationTokenSource _searchCts;

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
                
                if (_minPrice > 0)
                    queryParams.Add($"min_price={_minPrice}");
                if (_maxPrice > 0 && _maxPrice < 9999999)
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

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue;
            clearBtn.IsVisible = !string.IsNullOrEmpty(_searchText);
            
            // Debounce logic: cancel previous task and start a new one
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            Task.Run(async () => {
                try {
                    await Task.Delay(300, token);
                    MainThread.BeginInvokeOnMainThread(async () => await LoadTours());
                } catch (OperationCanceledException) { }
            }, token);
        }
        
        private async void OnTimeFilterChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            switch (selectedIndex)
            {
                case 0: // Tất cả
                    _minDuration = 0; _maxDuration = 0; break;
                case 1: // < 1 giờ
                    _minDuration = 0; _maxDuration = 60; break;
                case 2: // 1 - 2 giờ
                    _minDuration = 60; _maxDuration = 120; break;
                case 3: // 2 - 3 giờ
                    _minDuration = 120; _maxDuration = 180; break;
                case 4: // Trên 3 giờ
                    _minDuration = 181; _maxDuration = 9999; break;
            }

            await LoadTours();
        }

        private async void OnPriceFilterChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            switch (selectedIndex)
            {
                case 0: // Tất cả
                    _minPrice = 0; _maxPrice = 0; break;
                case 1: // < 100.000đ
                    _minPrice = 0; _maxPrice = 100000; break;
                case 2: // 100k - 250k
                    _minPrice = 100000; _maxPrice = 250000; break;
                case 3: // 250k - 500k
                    _minPrice = 250000; _maxPrice = 500000; break;
                case 4: // Trên 500.000đ
                    _minPrice = 500000; _maxPrice = 9999999; break;
            }

            await LoadTours();
        }

        private async void OnClearSearchClicked(object sender, EventArgs e)
        {
            searchEntry.Text = string.Empty;
            _searchText = string.Empty;
            _minDuration = 0;
            _maxDuration = 0;
            _minPrice = 0;
            _maxPrice = 0;
            
            if (timePicker != null) timePicker.SelectedIndex = 0;
            if (pricePicker != null) pricePicker.SelectedIndex = 0;

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