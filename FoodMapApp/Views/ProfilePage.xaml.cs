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
                ["prof_logout_btn"] = "Đăng xuất",
                ["prof_guest"] = "Khách",
                ["prof_not_provided"] = "Chưa cung cấp",
                ["prof_guest_acc"] = "Tài khoản Khách",
                ["prof_owner"] = "Chủ Nhà Hàng",
                ["prof_logout_confirm_title"] = "Đăng xuất",
                ["prof_logout_confirm_msg"] = "Bạn có chắc muốn đăng xuất?",
                ["prof_yes"] = "Có",
                ["prof_cancel"] = "Hủy",
                ["prof_audio_history"] = "Lịch sử nghe audio",
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            LogoutButton.Text = LocalizationService.Instance.Get("prof_logout_btn");
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
            
            // Hide audio history for guests
            AudioHistorySection.IsVisible = !isGuest;
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
    }
}
