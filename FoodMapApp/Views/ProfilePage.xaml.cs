using FoodMapApp.Services;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;
using System.Linq;

namespace FoodMapApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private const string BackendIp = "10.0.2.2";

        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Set all Vietnamese text from code-behind (Fixes "missing text" issue)
            VisitLabel.Text = "Quán đã ghé";
            ReviewLabel.Text = "Đánh giá";
            AreaLabel.Text = "Khu vực";
            HistoryTitleLabel.Text = "Lịch Sử Foodtour";
            HistoryEmptyLabel.Text = "Chưa ghé quán nào";
            ReviewTitleLabel.Text = "Đánh Giá Đã Viết";
            ReviewEmptyLabel.Text = "Chưa có lượt đánh giá nào";
            LogoutButton.Text = "Đăng Xuất";
            EditProfileButton.Text = "Chỉnh sửa";

            LoadProfileFromSession();
            await LoadHistory();
        }

        private void LoadProfileFromSession()
        {
            int userId = Preferences.Default.Get("user_id", 0);
            string username = Preferences.Default.Get("username", "Khách");
            string role = Preferences.Default.Get("role", "user");
            string email = Preferences.Default.Get("email", "");

            ProfileUsernameLabel.Text = username;
            ProfileEmailLabel.Text = email;
            ProfileRoleLabel.Text = role == "CNH" ? "Chủ Nhà Hàng" : role;

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

        private async Task LoadHistory()
        {
            int userId = Preferences.Default.Get("user_id", 0);
            if (userId == 0) return;

            string baseUrl = $"http://{BackendIp}:5000/api";

            try
            {
                var client = new HttpClient();
                var visits = await client.GetFromJsonAsync<List<VisitHistory>>($"{baseUrl}/food/history/{userId}");
                if (visits != null && visits.Any())
                {
                    HistoryCollection.ItemsSource = visits.Take(5).ToList();
                    VisitCountLabel.Text = visits.Count.ToString();
                }
                else
                {
                    HistoryCollection.ItemsSource = new List<VisitHistory>();
                    VisitCountLabel.Text = "0";
                }

                var reviews = await client.GetFromJsonAsync<List<ReviewHistory>>($"{baseUrl}/food/reviews/user/{userId}");
                if (reviews != null)
                {
                    ReviewsCollection.ItemsSource = reviews;
                    ReviewCountLabel.Text = reviews.Count.ToString();
                }
                else
                {
                    ReviewsCollection.ItemsSource = new List<ReviewHistory>();
                    ReviewCountLabel.Text = "0";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile history: {ex.Message}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Hủy");
            if (!confirm) return;

            var authService = new AuthService();
            authService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public class VisitHistory
    {
        public string? name { get; set; }
        public string? address { get; set; }
    }

    public class ReviewHistory
    {
        public string? comment { get; set; }
        public string? created_at { get; set; }
    }
}
