using FoodMapApp.Services;
using FoodMapApp.Models;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;

namespace FoodMapApp.Views
{
    public partial class TourHistoryPage : ContentPage
    {
        private readonly HttpClient _httpClient = new HttpClient();

        public TourHistoryPage()
        {
            InitializeComponent();
            refreshView.Command = new Command(async () => await LoadHistoryAsync());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LocalizeUI();
            await LoadHistoryAsync();
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["tour_hist_title"] = "Lịch sử đi Tour",
                ["tour_hist_empty"] = "Bạn chưa tham gia tour nào",
                ["tour_hist_shops"] = "Danh sách quán ăn",
                ["tour_hist_status"] = "Trạng thái",
                ["tour_hist_completed"] = "Đã hoàn thành",
                ["tour_hist_partial"] = "Đang thực hiện",
                ["tour_hist_date"] = "Ngày đi",
                ["tour_hist_view_map"] = "Xem trên bản đồ"
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            PageTitleLabel.Text = LocalizationService.Instance.Get("tour_hist_title");
            EmptyLabel.Text = LocalizationService.Instance.Get("tour_hist_empty");
        }

        private async Task LoadHistoryAsync()
        {
            try
            {
                LoadingOverlay.IsVisible = true;
                HistoryContainer.Children.Clear();
                EmptyView.IsVisible = false;

                int userId = Preferences.Default.Get("user_id", 0);
                if (userId == 0) return;

                var response = await _httpClient.GetAsync($"{AppConfig.BaseUrl}/Tours/history/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var histories = JsonSerializer.Deserialize<List<TourHistoryModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (histories == null || !histories.Any())
                    {
                        EmptyView.IsVisible = true;
                        return;
                    }

                    foreach (var h in histories.OrderByDescending(x => x.CreatedAt))
                    {
                        var card = CreateTourHistoryCard(h);
                        HistoryContainer.Children.Add(card);
                    }
                }
                else
                {
                    EmptyView.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistory Error: {ex.Message}");
                EmptyView.IsVisible = true;
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
                refreshView.IsRefreshing = false;
            }
        }

        private View CreateTourHistoryCard(TourHistoryModel h)
        {
            var border = new Border
            {
                Padding = 12,
                Margin = new Thickness(0, 0, 0, 15),
                StrokeThickness = 0,
                BackgroundColor = Colors.White,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 }
            };
            border.Shadow = new Shadow { Brush = Color.FromArgb("#FF6B81"), Offset = new Point(0, 4), Opacity = 0.08f, Radius = 10 };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 15 };

            // Icon Column (Left)
            var iconBorder = new Border { BackgroundColor = Color.FromArgb("#FFF0F3"), WidthRequest = 50, HeightRequest = 50, StrokeThickness = 0 };
            iconBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 };
            iconBorder.Content = new Microsoft.Maui.Controls.Shapes.Path { 
                Data = (Microsoft.Maui.Controls.Shapes.Geometry)new Microsoft.Maui.Controls.Shapes.PathGeometryConverter().ConvertFromInvariantString("M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 0 1 0-5 2.5 2.5 0 0 1 0 5z"),
                Fill = Color.FromArgb("#FF6B81"), Aspect = Stretch.Uniform, WidthRequest = 24, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center 
            };
            grid.Children.Add(iconBorder);

            // Middle Column (Info)
            var infoStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
            infoStack.Children.Add(new Label { Text = h.TourName, FontAttributes = FontAttributes.Bold, FontSize = 17, TextColor = Color.FromArgb("#333") });
            infoStack.Children.Add(new Label { Text = $"{LocalizationService.Instance.Get("tour_hist_date")}: {h.CreatedAt?.ToString("dd/MM/yyyy")}", FontSize = 12, TextColor = Colors.Gray });
            grid.Add(infoStack, 1, 0);

            // Right Column (Status)
            bool isFull = h.Status?.Contains("100") == true || h.Status?.ToLower().Contains("completed") == true;
            string statusStr = isFull ? LocalizationService.Instance.Get("tour_hist_completed") : LocalizationService.Instance.Get("tour_hist_partial");
            
            var statusBorder = new Border { BackgroundColor = Color.FromArgb("#FFF0F3"), Padding = new Thickness(10, 4), VerticalOptions = LayoutOptions.Center, StrokeThickness = 0 };
            statusBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 };
            statusBorder.Content = new Label { 
                Text = isFull ? statusStr : $"{(int)h.ProgressPercentage}%", 
                TextColor = isFull ? Colors.Green : Color.FromArgb("#FF6B81"), 
                FontSize = 11, FontAttributes = FontAttributes.Bold 
            };
            grid.Add(statusBorder, 2, 0);

            border.Content = grid;
            return border;
        }

        private async Task PopulateShopsListAsync(VerticalStackLayout container, int tourId)
        {
            try
            {
                var json = await _httpClient.GetStringAsync($"{AppConfig.BaseUrl}/Tours/{tourId}");
                var tour = JsonSerializer.Deserialize<TourDetailModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (tour != null && tour.TourPois != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() => {
                        container.Children.Clear();
                        foreach (var tp in tour.TourPois.OrderBy(x => x.OrderIndex))
                        {
                            var shopLabel = new Label { 
                                Text = $"• {tp.Poi?.name ?? tp.PoiName ?? "Quán ăn"}", 
                                FontSize = 13, 
                                TextColor = Color.FromArgb("#555") 
                            };
                            container.Children.Add(shopLabel);
                        }
                    });
                }
            }
            catch { /* Ignore error for detail sub-loading */ }
        }

        private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
}
