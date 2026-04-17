using FoodMapApp.Services;
using System.Net.Http.Json;
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

            // Set all Vietnamese text from code-behind (Fixes "missing text" issue)


            LogoutButton.Text = "Đăng Xuất";
            EditProfileButton.Text = "Chỉnh sửa";

            LoadProfileFromSession();
            await LoadTourHistoryAsync();
        }

        private void LoadProfileFromSession()
        {
            var authService = new AuthService();
            bool isGuest = authService.IsGuest;

            string username = Preferences.Default.Get("username", "Khách");
            string role = Preferences.Default.Get("role", "user");
            string email = Preferences.Default.Get("email", "");

            ProfileUsernameLabel.Text = username;
            ProfileEmailLabel.Text = isGuest ? "Chưa cung cấp" : email;
            ProfileRoleLabel.Text = role == "CNH" ? "Chủ Nhà Hàng" : (isGuest ? "Tài khoản Khách" : role);
            
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
                await DisplayAlert("Lỗi", "Vui lòng nhập đầy đủ tên và email.", "OK");
                return;
            }

            int userId = Preferences.Default.Get("user_id", 0);
            if (userId == 0) return;

            var authService = new AuthService();
            bool success = await authService.UpdateProfileAsync(userId, newUsername, newEmail, string.IsNullOrEmpty(newPassword) ? null : newPassword);

            if (success)
            {
                await DisplayAlert("Thành công", "Thông tin đã được cập nhật.", "OK");
                DisplayModeLayout.IsVisible = true;
                EditModeLayout.IsVisible = false;
                LoadProfileFromSession();
            }
            else
            {
                await DisplayAlert("Lỗi", "Không thể cập nhật thông tin. Vui lòng thử lại.", "OK");
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
                        Text = "Lịch sử tour chỉ dành cho thành viên.", 
                        TextColor = Colors.Gray, 
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 20)
                    });
                    return;
                }

                if (userId == 0)

                tourHistoryContainer.Children.Clear();
                tourHistoryContainer.Children.Add(new Label { Text = "Đang tải...", TextColor = Colors.Gray, FontAttributes = FontAttributes.Italic, HorizontalOptions = LayoutOptions.Center });

                using HttpClient client = new HttpClient();
                var response = await client.GetAsync($"{AppConfig.BaseUrl}/Tours/history/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var histories = System.Text.Json.JsonSerializer.Deserialize<List<TourHistoryModel>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    tourHistoryContainer.Children.Clear();

                    if (histories == null || !histories.Any())
                    {
                        tourHistoryContainer.Children.Add(new Label { Text = "Chưa có lịch sử đi tour nào.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
                        return;
                    }

                    foreach (var h in histories)
                    {
                        var frame = new Border
                        {
                            StrokeThickness = 1,
                            Stroke = Color.FromArgb("#FF6B81"),
                            BackgroundColor = Color.FromArgb("#FFF0F3"),
                            Padding = new Thickness(15),
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) }
                        };
                        
                        var layout = new VerticalStackLayout { Spacing = 5 };
                        layout.Children.Add(new Label { Text = h.TourName, FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = Color.FromArgb("#333") });
                        layout.Children.Add(new Label { Text = $"Trạng thái: {(h.Status.Contains("100") ? "Đã hoàn thành" : "Hoàn thành 50%")}", FontSize = 13, TextColor = h.Status.Contains("100") ? Colors.Green : Color.FromArgb("#FF9F43") });
                        layout.Children.Add(new Label { Text = $"Ngày đi: {h.CreatedAt:dd/MM/yyyy}", FontSize = 12, TextColor = Colors.Gray });

                        frame.Content = layout;
                        tourHistoryContainer.Children.Add(frame);
                    }
                }
            }
            catch (Exception ex)
            {
                tourHistoryContainer.Children.Clear();
                tourHistoryContainer.Children.Add(new Label { Text = "Lỗi khi tải lịch sử.", TextColor = Colors.Red, HorizontalOptions = LayoutOptions.Center });
                System.Diagnostics.Debug.WriteLine($"LoadHistory error: {ex.Message}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Hủy");
            if (!confirm) return;

            var authService = new AuthService();
            await authService.LogoutAsync();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public class TourHistoryModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TourId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal ProgressPercentage { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string TourName { get; set; } = string.Empty;
    }
}
