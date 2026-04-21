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
            
            // Hide features for guests
            EditProfileButton.IsVisible = !isGuest;
            TourHistorySection.IsVisible = !isGuest;
            AudioHistorySection.IsVisible = !isGuest;

            EditUsernameEntry.Text = username;
            EditEmailEntry.Text = email;
            EditPasswordEntry.Text = "";
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            if (new AuthService().IsGuest) return;
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
            var authService = new AuthService();
            if (authService.IsGuest) return;

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
            if (new AuthService().IsGuest) return;
            await Navigation.PushAsync(new AudioHistoryPage());
        }

        private async void OnHistoryHeaderClicked(object sender, EventArgs e)
        {
            if (new AuthService().IsGuest) return;
            await Navigation.PushAsync(new TourHistoryPage());
        }
    }
}
