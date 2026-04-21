using System.Diagnostics;
using System.Text.Json;
using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp
{
    public partial class MainPage
    {
        private List<TourModel>? _tours = null;
        private TourDetailModel? _currentTour = null;

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            tourDrawerOverlay.IsVisible = true;
            await tourDrawer.TranslateTo(0, 0, 250, Easing.CubicOut);
            if (_tours == null) await LoadToursAsync();
        }

        private async void OnCloseDrawerClicked(object sender, EventArgs e)
        {
            await tourDrawer.TranslateTo(300, 0, 250, Easing.CubicIn);
            tourDrawerOverlay.IsVisible = false;
        }

        private async Task LoadToursAsync()
        {
            try
            {
                tourListContainer.Children.Clear();
                tourListContainer.Children.Add(new Label { Text = LocalizationService.Instance.Get("main_tour_loading"), TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Margin = 20 });

                var response = await HttpService.Client.GetAsync($"{AppConfig.BaseUrl}/Tours");
                tourListContainer.Children.Clear();

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _tours = JsonSerializer.Deserialize<List<TourModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (_tours == null || !_tours.Any())
                    {
                        tourListContainer.Children.Add(new Label { Text = LocalizationService.Instance.Get("main_tour_empty"), TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center, Margin = 20 });
                        return;
                    }

                    foreach (var tour in _tours.OrderByDescending(t => t.Id))
                    {
                        var frame = new Border { StrokeThickness = 1, Stroke = Color.FromArgb("#E0E0E0"), BackgroundColor = Colors.White, Padding = 15, Margin = new Thickness(0,0,0,10), StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 } };
                        var stack = new VerticalStackLayout { Spacing = 5 };
                        stack.Children.Add(new Label { Text = tour.Name, FontAttributes = FontAttributes.Bold, FontSize = 16 });
                        stack.Children.Add(new Label { Text = tour.Description, FontSize = 13, TextColor = Colors.Gray });
                        var btn = new Button { Text = LocalizationService.Instance.Get("main_tour_view_btn"), HeightRequest = 40, CornerRadius = 20, BackgroundColor = Color.FromArgb("#FF3B5C"), Margin = new Thickness(0,10,0,0) };
                        btn.Clicked += async (s, e) => await StartTourModeAsync(tour.Id);
                        stack.Children.Add(btn);
                        frame.Content = stack;
                        tourListContainer.Children.Add(frame);
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"LoadTours Error: {ex.Message}"); }
        }

        private async Task StartTourModeAsync(int tourId)
        {
            try
            {
                var json = await HttpService.GetStringAsync($"{AppConfig.BaseUrl}/Tours/{tourId}");
                if (json != null)
                {
                    _currentTour = JsonSerializer.Deserialize<TourDetailModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (_currentTour != null)
                    {
                        await tourDrawer.TranslateTo(300, 0, 250, Easing.CubicIn);
                        tourDrawerOverlay.IsVisible = false;
                        simTourName.Text = _currentTour.Name;
                        simTotalTime.Text = LocalizationService.Instance.Get("main_calculating");
                        simTotalPrice.Text = LocalizationService.Instance.Get("main_calculating");
                        simProgress.Text = LocalizationService.Instance.Get("main_tour_not_moved");
                        tourSimulationPanel.IsVisible = true;
                        btnSimulateStart.IsVisible = true;
                        btnSimulateNext.IsVisible = false;
                        var jsArray = JsonSerializer.Serialize(_currentTour.TourPois);
                        mapView.EvaluateJavaScriptAsync($"window.startTourRoute({jsArray}, {_currentTour.Id})");
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"StartTourMode error: {ex.Message}"); }
        }

        private async Task SaveTourHistoryLocallyAsync(int tourId, decimal progressPercentage, string status)
        {
            try
            {
                if (new AuthService().IsGuest) return;
                int userId = Preferences.Default.Get("user_id", 0);
                if (userId == 0) return;
                var payload = new { UserId = userId, TourId = tourId, ProgressPercentage = progressPercentage, Status = status };
                using HttpClient client = new HttpClient();
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync($"{AppConfig.BaseUrl}/Tours/history", content);
            }
            catch (Exception ex) { Debug.WriteLine($"Tour History Save Error: {ex.Message}"); }
        }

        private void UpdateTourProgressUI(string duration, string price, string progress, string durPrefix = "", string pricePrefix = "")
        {
            if (!string.IsNullOrEmpty(durPrefix)) simTimeLabelPrefix.Text = durPrefix;
            if (!string.IsNullOrEmpty(pricePrefix)) simPriceLabelPrefix.Text = pricePrefix;
            simTotalTime.Text = duration;
            simTotalPrice.Text = price;
            simProgress.Text = progress;
        }

        private async void OnSimulateStartClicked(object sender, EventArgs e)
        {
            btnSimulateStart.IsVisible = false;
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("window.simulateTourNextStop()");
        }

        private async void OnSimulateArriveClicked(object sender, EventArgs e)
        {
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("window.simulateTourArrive()");
        }

        private async void OnSimulateNextClicked(object sender, EventArgs e)
        {
            if (mapView != null) await mapView.EvaluateJavaScriptAsync("window.simulateTourNextStop()");
        }

        private void OnEndTourClicked(object sender, EventArgs e)
        {
            tourSimulationPanel.IsVisible = false;
            if (mapView != null) mapView.EvaluateJavaScriptAsync("window.endTourRoute()");
        }
    }
}
