using FoodMapApp.Services;

namespace FoodMapApp.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AuthService _authService;

        public RegisterPage()
        {
            InitializeComponent();
            _authService = new AuthService();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LocalizeUI();
        }

        private async Task LocalizeUI()
        {
            var source = new Dictionary<string, string>
            {
                ["reg_title"] = "Tham Gia FoodMap",
                ["reg_subtitle"] = "Tạo tài khoản để khám phá ẩm thực Vĩnh Khánh.",
                ["reg_user_lbl"] = "Tên đăng nhập",
                ["reg_user_ph"] = "Chọn tên đăng nhập",
                ["reg_email_lbl"] = "Địa chỉ Email",
                ["reg_pwd_lbl"] = "Mật khẩu",
                ["reg_confirm_pwd_lbl"] = "Xác nhận mật khẩu",
                ["reg_btn"] = "ĐĂNG KÝ",
                ["reg_have_acc"] = "Đã có tài khoản?",
                ["reg_login_link"] = "Đăng nhập",
                ["reg_err"] = "Lỗi",
                ["reg_missing_fields"] = "Vui lòng điền đầy đủ thông tin.",
                ["reg_pwd_mismatch"] = "Mật khẩu xác nhận không khớp.",
                ["reg_success"] = "Thành công",
                ["reg_welcome"] = "Tài khoản đã tạo! Chào mừng đến FoodMap Vĩnh Khánh.",
                ["reg_fail_title"] = "Đăng ký thất bại"
            };

            await LocalizationService.Instance.InitializeAsync(Preferences.Default.Get("app_lang", "vi"), source);

            TitleLabel.Text = LocalizationService.Instance.Get("reg_title");
            SubtitleLabel.Text = LocalizationService.Instance.Get("reg_subtitle");
            UsernameLbl.Text = LocalizationService.Instance.Get("reg_user_lbl");
            UsernameEntry.Placeholder = LocalizationService.Instance.Get("reg_user_ph");
            EmailLbl.Text = LocalizationService.Instance.Get("reg_email_lbl");
            PasswordLbl.Text = LocalizationService.Instance.Get("reg_pwd_lbl");
            ConfirmPasswordLbl.Text = LocalizationService.Instance.Get("reg_confirm_pwd_lbl");
            RegisterButton.Text = LocalizationService.Instance.Get("reg_btn");
            HaveAccountLabel.Text = LocalizationService.Instance.Get("reg_have_acc");
            LoginNowLabel.Text = LocalizationService.Instance.Get("reg_login_link");
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;
            string confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert(LocalizationService.Instance.Get("reg_err"), LocalizationService.Instance.Get("reg_missing_fields"), "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert(LocalizationService.Instance.Get("reg_err"), LocalizationService.Instance.Get("reg_pwd_mismatch"), "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            var result = await _authService.RegisterAsync(username, email, password);
            LoadingIndicator.IsRunning = false;

            if (result.success)
            {
                await DisplayAlert(LocalizationService.Instance.Get("reg_success"), LocalizationService.Instance.Get("reg_welcome"), "OK");
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                await DisplayAlert(LocalizationService.Instance.Get("reg_fail_title"), result.message, "OK");
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
