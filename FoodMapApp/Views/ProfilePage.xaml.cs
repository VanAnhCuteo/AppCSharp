using FoodMapApp.Services;
using FoodMapApp.Models;
using System.Net.Http.Json;
using System.Net.Http;
using Microsoft.Maui.Storage;
using System.Linq;

namespace FoodMapApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LocalizeUI();
            
            LoadProfileFromSession();
            await LoadTourHistoryAsync();
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["prof_edit_btn"] = "Chỉnh sửa",
                ["prof_logout_btn"] = "Đăng xuất",
                ["prof_guest"] = "Khách",
                ["prof_not_provided"] = "Chưa cung cấp",
                ["prof_guest_acc"] = "Tài khoản Khách",
                ["prof_owner"] = "Chủ Nhà Hàng",
                ["prof_history_title"] = "Lịch Sử Đi Tour Ẩm Thực",
                ["prof_history_guest"] = "Lịch sử tour chỉ dành cho thành viên.",
                ["prof_loading"] = "Đang tải...",
                ["prof_no_history"] = "Chưa có lịch sử đi tour nào.",
                ["prof_status"] = "Trạng thái",
                ["prof_completed"] = "Đã hoàn thành",
                ["prof_partial"] = "Hoàn thành 50%",
                ["prof_date"] = "Ngày đi",
                ["prof_error_load"] = "Lỗi khi tải lịch sử.",
                ["prof_logout_confirm_title"] = "Đăng xuất",
                ["prof_logout_confirm_msg"] = "Bạn có chắc muốn đăng xuất?",
                ["prof_yes"] = "Có",
                ["prof_cancel"] = "Hủy",
                ["prof_audio_history"] = "Lịch sử nghe audio",
                ["prof_save_err"] = "Lỗi",
                ["prof_missing_info"] = "Vui lòng nhập đầy đủ tên và email.",
                ["prof_success"] = "Thành công",
                ["prof_updated"] = "Thông tin đã được cập nhật.",
                ["prof_update_fail"] = "Không thể cập nhật thông tin. Vui lòng thử lại."
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            LogoutButton.Text = LocalizationService.Instance.Get("prof_logout_btn");
            EditProfileButton.Text = LocalizationService.Instance.Get("prof_edit_btn");
            HistoryHeaderLabel.Text = LocalizationService.Instance.Get("prof_history_title");
        }

        private void LoadProfileFromSession()
        {
            var authService = new AuthService();
            bool isGuest = authService.IsGuest;

            string username = Preferences.Default.Get("username", "Khách");
            string role = Preferences.Default.Get("role", "user");
            string email = Preferences.Default.Get("email", "");

            ProfileUsernameLabel.Text = username;
            ProfileEmailLabel.Text = isGuest ? LocalizationService.Instance.Get("prof_not_provided") : email;
            
            string roleDisplay = role;
            if (role == "CNH") roleDisplay = LocalizationService.Instance.Get("prof_owner");
            else if (isGuest) roleDisplay = LocalizationService.Instance.Get("prof_guest_acc");
            
            ProfileRoleLabel.Text = roleDisplay;
            
            // Hide edit button for guests
            EditProfileButton.IsVisible = !isGuest;

            EditUsernameEntry.Text = username;
            EditEmailEntry.Text = email;
            EditPasswordEntry.Text = "";
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            DisplayModeLayout.IsVisible = false;
            EditModeLayout.IsVisible = true;
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            DisplayModeLayout.IsVisible = true;
            EditModeLayout.IsVisible = false;
            LoadProfileFromSession(); // Reset entries
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string newUsername = EditUsernameEntry.Text?.Trim();
            string newEmail = EditEmailEntry.Text?.Trim();
            string newPassword = EditPasswordEntry.Text?.Trim();

            if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newEmail))
            {
                await DisplayAlert(LocalizationService.Instance.Get("prof_save_err"), LocalizationService.Instance.Get("prof_missing_info"), "OK");
                return;
            }

            int userId = Preferences.Default.Get("user_id", 0);
            if (userId == 0) return;

            var authService = new AuthService();
            bool success = await authService.UpdateProfileAsync(userId, newUsername, newEmail, string.IsNullOrEmpty(newPassword) ? null : newPassword);

            if (success)
            {
                await DisplayAlert(LocalizationService.Instance.Get("prof_success"), LocalizationService.Instance.Get("prof_updated"), "OK");
                DisplayModeLayout.IsVisible = true;
                EditModeLayout.IsVisible = false;
                LoadProfileFromSession();
            }
            else
            {
                await DisplayAlert(LocalizationService.Instance.Get("prof_save_err"), LocalizationService.Instance.Get("prof_update_fail"), "OK");
            }
        }



        private async Task LoadTourHistoryAsync()
        {
            try
            {
                int userId = Preferences.Default.Get("user_id", 0);
                var authService = new AuthService();

                if (authService.IsGuest)
                {
                    tourHistoryContainer.Children.Clear();
                    tourHistoryContainer.Children.Add(new Label { 
                        Text = LocalizationService.Instance.Get("prof_history_guest"), 
                        TextColor = Colors.Gray, 
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    });
                    return;
                }

                if (userId == 0)

                tourHistoryContainer.Children.Clear();
                tourHistoryContainer.Children.Add(new Label { Text = LocalizationService.Instance.Get("prof_loading"), TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic, HorizontalOptions = LayoutOptions.Center });

                using HttpClient client = new HttpClient();
                var response = await client.GetAsync($"{AppConfig.BaseUrl}/Tours/history/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var histories = System.Text.Json.JsonSerializer.Deserialize<List<TourHistoryModel>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    tourHistoryContainer.Children.Clear();

                    if (histories == null || !histories.Any())
                    {
                        tourHistoryContainer.Children.Add(new Label { Text = LocalizationService.Instance.Get("prof_no_history"), TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
                        return;
                    }

                    foreach (var h in histories)
                    {
                        var frame = new Border
                        {
                            StrokeThickness = 0,
                            BackgroundColor = Colors.White,
                            Padding = new Thickness(12),
                            Margin = new Thickness(0, 0, 0, 15),
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(20) }
                        };
                        frame.Shadow = new Shadow { Brush = Color.FromArgb("#FF6B81"), Offset = new Point(0, 4), Opacity = 0.08f, Radius = 10 };
                        
                        var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 15 };

                        // Icon Column
                        var iconBorder = new Border { BackgroundColor = Color.FromArgb("#FFF0F3"), WidthRequest = 44, HeightRequest = 44, StrokeThickness = 0 };
                        iconBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) };
                        iconBorder.Content = new Microsoft.Maui.Controls.Shapes.Path { 
                            Data = (Microsoft.Maui.Controls.Shapes.Geometry)new Microsoft.Maui.Controls.Shapes.PathGeometryConverter().ConvertFromInvariantString("M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5a2.5 2.5 0 0 1 0-5 2.5 2.5 0 0 1 0 5z"),
                            Fill = Color.FromArgb("#FF6B81"), Aspect = Stretch.Uniform, WidthRequest = 22, HeightRequest = 22, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center 
                        };
                        grid.Children.Add(iconBorder);

                        // Info Column
                        var infoStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
                        infoStack.Children.Add(new Label { Text = h.TourName, FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = Color.FromArgb("#333") });
                        infoStack.Children.Add(new Label { Text = h.CreatedAt?.ToString("dd/MM/yyyy") ?? "", FontSize = 12, TextColor = Colors.Gray });
                        grid.Add(infoStack, 1, 0);

                        // Status Column
                        bool isCompleted = h.Status?.Contains("100") == true;
                        var statusBorder = new Border { BackgroundColor = Color.FromArgb("#FFF0F3"), Padding = new Thickness(10, 4), VerticalOptions = LayoutOptions.Center, StrokeThickness = 0 };
                        statusBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) };
                        statusBorder.Content = new Label { 
                            Text = isCompleted ? LocalizationService.Instance.Get("prof_completed") : $"{(int)h.ProgressPercentage}%", 
                            TextColor = isCompleted ? Colors.Green : Color.FromArgb("#FF6B81"), 
                            FontSize = 11, FontAttributes = FontAttributes.Bold 
                        };
                        grid.Add(statusBorder, 2, 0);

                        frame.Content = grid;
                        tourHistoryContainer.Children.Add(frame);
                    }
                }
            }
            catch (Exception ex)
            {
                tourHistoryContainer.Children.Clear();
                tourHistoryContainer.Children.Add(new Label { Text = LocalizationService.Instance.Get("prof_error_load"), TextColor = Colors.Red, HorizontalOptions = LayoutOptions.Center });
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                LocalizationService.Instance.Get("prof_logout_confirm_title"), 
                LocalizationService.Instance.Get("prof_logout_confirm_msg"), 
                LocalizationService.Instance.Get("prof_yes"), 
                LocalizationService.Instance.Get("prof_cancel"));
            if (!confirm) return;

            var authService = new AuthService();
            await authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }

        private async void OnAudioHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AudioHistoryPage());
        }

        private async void OnHistoryHeaderClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TourHistoryPage());
        }
    }
}
