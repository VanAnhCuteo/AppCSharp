using FoodMapApp.Services;
using FoodMapApp.Models;

namespace FoodMapApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;

        public LoginPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // 0. Handle Pending Guest Login from Deep Link
            if (App.PendingGuestLogin)
            {
                App.PendingGuestLogin = false; // Reset
                int guestId = new Random().Next(100000, 999999);
                _authService.LoginAsGuest(guestId);
                await Shell.Current.GoToAsync("//MainTabs");
                return;
            }

            // 1. Skip login if already authenticated
            if (_authService.IsLoggedIn)
            {
                // Nếu đang có Deep Link chờ mở quán (POI), không tự động nhảy về HomePage
                // LUỒNG POI: Để App.xaml.cs tự điều hướng tới MainPage
                if (MainPage.PendingOpenFoodId.HasValue || !string.IsNullOrEmpty(App.PendingDeepLinkUri))
                {
                    System.Diagnostics.Debug.WriteLine("Deep link (POI/Audio) detected, letting App.xaml.cs handle navigation");
                    return;
                }

                // LUỒNG BÌNH THƯỜNG: Tự động vào HomePage
                await Shell.Current.GoToAsync("//MainTabs");
                return;
            }

            _ = LocalizationService.Instance.RefreshLanguagesAsync(); // Background fetch
            await LocalizeUI();

            // Slide out animation for the language bubble
            await Task.Delay(500); // Wait for page to settle
            await Task.WhenAll(
                LangBubble.TranslateTo(0, 0, 800, Easing.SpringOut),
                LangBubble.FadeTo(1, 600)
            );
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["login_title"] = "FoodMap Vĩnh Khánh",
                ["login_welcome"] = "Khám phá ẩm thực quanh bạn",
                ["login_guest_btn"] = "TÀI KHOẢN KHÁCH",
                ["login_or"] = "HOẶC",
                ["login_offline_btn"] = "ĐĂNG NHẬP OFFLINE",
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            AppTitleLabel.Text = LocalizationService.Instance.Get("login_title");
            WelcomeLabel.Text = LocalizationService.Instance.Get("login_welcome");
            QrScanButton.Text = LocalizationService.Instance.Get("login_guest_btn");
            OrLabel.Text = LocalizationService.Instance.Get("login_or");
            OfflineButton.Text = LocalizationService.Instance.Get("login_offline_btn");

            // Update Language Bubble Code
            string lang = LocalizationService.Instance.CurrentLanguage;
            LangCodeLabel.Text = lang switch
            {
                "vi" => "VN",
                "en" => "EN",
                "ko" => "KR",
                "ja" => "JA",
                "zh" => "ZH",
                _ => lang.Length >= 2 ? lang.Substring(0, 2).ToUpper() : lang.ToUpper()
            };
        }

        private async void OnOfflineClicked(object sender, EventArgs e)
        {
            _authService.LoginOffline();
            await Shell.Current.GoToAsync("//MainTabs");
        }

        private async void OnGuestClicked(object sender, EventArgs e)
        {
            int guestId = new Random().Next(100000, 999999);
            _authService.LoginAsGuest(guestId);
            await Shell.Current.GoToAsync("//MainTabs");
        }

        private async void OnQrScanClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("QrScannerPage");
        }

        private async void OnLanguageClicked(object sender, EventArgs e)
        {
            try
            {
                var languages = LocalizationService.Instance.AvailableLanguages;
                if (languages == null || !languages.Any())
                {
                    await LocalizationService.Instance.RefreshLanguagesAsync();
                    languages = LocalizationService.Instance.AvailableLanguages;
                }

                string[] options = languages.Select(l => l.name).ToArray();
                string cancel = LocalizationService.Instance.Get("cancel", "Hủy");
                string title = LocalizationService.Instance.Get("select_lang", "Chọn ngôn ngữ");

                string result = await DisplayActionSheet(title, cancel, null, options);
                if (result != null && result != cancel)
                {
                    var selected = languages.FirstOrDefault(l => l.name == result);
                    if (selected != null)
                    {
                        Preferences.Default.Set("app_lang", selected.language_code);
                        LocalizationService.Instance.CurrentLanguage = selected.language_code;
                        await LocalizeUI();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error selecting language: {ex.Message}");
            }
        }
    }
}
