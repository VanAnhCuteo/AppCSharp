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



        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Hủy");
            if (!confirm) return;

            var authService = new AuthService();
            authService.Logout();
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }


}
