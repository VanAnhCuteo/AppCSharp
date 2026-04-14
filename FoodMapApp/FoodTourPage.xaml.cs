using System.Collections.ObjectModel;
using FoodMapApp.Services;
using System.Net.Http.Json;
using FoodMapApp.Models;
using System.Diagnostics;
using System.Threading;

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

        // Bottom Sheet State
        private TourModel _selectedTour;

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
                
                // Cache key includes query params to store different filter results
                string cacheKey = "tours_list_" + url.GetHashCode();

                var tours = await HttpService.GetWithCacheAsync<List<TourModel>>(url, cacheKey);
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
                case 0: _minDuration = 0; _maxDuration = 0; break;
                case 1: _minDuration = 0; _maxDuration = 60; break;
                case 2: _minDuration = 60; _maxDuration = 120; break;
                case 3: _minDuration = 120; _maxDuration = 180; break;
                case 4: _minDuration = 181; _maxDuration = 9999; break;
            }
            await LoadTours();
        }

        private async void OnPriceFilterChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;
            switch (selectedIndex)
            {
                case 0: _minPrice = 0; _maxPrice = 0; break;
                case 1: _minPrice = 0; _maxPrice = 100000; break;
                case 2: _minPrice = 100000; _maxPrice = 250000; break;
                case 3: _minPrice = 250000; _maxPrice = 500000; break;
                case 4: _minPrice = 500000; _maxPrice = 9999999; break;
            }
            await LoadTours();
        }

        private async void OnClearSearchClicked(object sender, EventArgs e)
        {
            searchEntry.Text = string.Empty;
            _searchText = string.Empty;
            _minDuration = 0; _maxDuration = 0;
            _minPrice = 0; _maxPrice = 0;
            
            if (timePicker != null) timePicker.SelectedIndex = 0;
            if (pricePicker != null) pricePicker.SelectedIndex = 0;

            await LoadTours();
        }

        // --- Bottom Sheet & Detail Logic ---

        private void OnTourSelected(object sender, TappedEventArgs e)
        {
            if (e.Parameter is TourModel tour)
            {
                _selectedTour = tour;
                UpdateBottomSheetUI();
                OpenBottomSheet();
            }
        }

        private void UpdateBottomSheetUI()
        {
            if (_selectedTour == null) return;
            sheetTitle.Text = _selectedTour.name;
            sheetDesc.Text = _selectedTour.description;
            sheetTime.Text = _selectedTour.DurationDisplay;
            sheetPrice.Text = _selectedTour.PriceDisplay;
        }

        private async void OpenBottomSheet()
        {
            dimOverlay.IsVisible = true;
            _ = dimOverlay.FadeTo(0.6, 250);
            await bottomSheet.TranslateTo(0, 0, 300, Easing.CubicOut);
        }

        private async void OnCloseBottomSheet(object sender, EventArgs e)
        {
            _ = dimOverlay.FadeTo(0, 250);
            await bottomSheet.TranslateTo(0, 1000, 300, Easing.CubicIn);
            dimOverlay.IsVisible = false;
        }

        private async void OnStartJourneyClicked(object sender, EventArgs e)
        {
            dimOverlay.IsVisible = false;
            dimOverlay.Opacity = 0;
            bottomSheet.TranslationY = 1000;

            if (_selectedTour != null)
            {
                await Navigation.PushAsync(new Views.TourMapPage(_selectedTour.tour_id));
            }
        }


    }
}