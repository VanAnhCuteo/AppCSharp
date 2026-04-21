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
                // để tránh xung đột với lệnh điều hướng từ App.xaml.cs
                if (MainPage.PendingOpenFoodId.HasValue || !string.IsNullOrEmpty(App.PendingDeepLinkUri))
                {
                    System.Diagnostics.Debug.WriteLine("Deep link detected, skipping auto-redirect to HomePage");
                    return;
                }

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
                ["login_welcome"] = "Chào mừng trở lại! Vui lòng đăng nhập.",
                ["login_id_label"] = "Tên đăng nhập hoặc Email",
                ["login_id_ph"] = "Nhập tên đăng nhập",
                ["login_pwd_label"] = "Mật khẩu",
                ["login_btn"] = "ĐĂNG NHẬP",
                ["login_guest_btn"] = "TÀI KHOẢN KHÁCH",
                ["login_or"] = "HOẶC",
                ["login_offline_btn"] = "ĐĂNG NHẬP OFFLINE",
                ["login_no_acc"] = "Chưa có tài khoản?",
                ["login_register_link"] = "Đăng ký ngay",
                ["login_err"] = "Lỗi",
                ["login_missing_fields"] = "Vui lòng nhập tên đăng nhập và mật khẩu.",
                ["login_fail_title"] = "Đăng nhập thất bại"
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            AppTitleLabel.Text = LocalizationService.Instance.Get("login_title");
            WelcomeLabel.Text = LocalizationService.Instance.Get("login_welcome");
            IdentifierLabel.Text = LocalizationService.Instance.Get("login_id_label");
            IdentifierEntry.Placeholder = LocalizationService.Instance.Get("login_id_ph");
            PasswordLabel.Text = LocalizationService.Instance.Get("login_pwd_label");
            LoginButton.Text = LocalizationService.Instance.Get("login_btn");
            QrScanButton.Text = LocalizationService.Instance.Get("login_guest_btn");
            OrLabel.Text = LocalizationService.Instance.Get("login_or");
            OfflineButton.Text = LocalizationService.Instance.Get("login_offline_btn");
            NoAccountLabel.Text = LocalizationService.Instance.Get("login_no_acc");
            RegisterNowLabel.Text = LocalizationService.Instance.Get("login_register_link");

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

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string identifier = IdentifierEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert(LocalizationService.Instance.Get("login_err"), LocalizationService.Instance.Get("login_missing_fields"), "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            var result = await _authService.LoginAsync(identifier, password);
            LoadingIndicator.IsRunning = false;

            if (result.success)
            {
                await Shell.Current.GoToAsync("//MainTabs");
            }
            else
            {
                await DisplayAlert(LocalizationService.Instance.Get("login_fail_title"), result.message, "OK");
            }
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

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }
    }
}
