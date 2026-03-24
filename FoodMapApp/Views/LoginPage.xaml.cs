using FoodMapApp.Services;

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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Set Vietnamese text from code-behind to avoid XAML encoding issues
            AppTitleLabel.Text = "FoodTour Bùi Viện";
            WelcomeLabel.Text = "Chào mừng trở lại! Vui lòng đăng nhập.";
            IdentifierLabel.Text = "Tên đăng nhập hoặc Email";
            IdentifierEntry.Placeholder = "Nhập tên đăng nhập";
            PasswordLabel.Text = "Mật khẩu";
            LoginButton.Text = "ĐĂNG NHẬP";
            NoAccountLabel.Text = "Chưa có tài khoản?";
            RegisterNowLabel.Text = "Đăng ký ngay";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string identifier = IdentifierEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng nhập tên đăng nhập và mật khẩu.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            var result = await _authService.LoginAsync(identifier, password);
            LoadingIndicator.IsRunning = false;

            if (result.success)
            {
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                await DisplayAlert("Đăng nhập thất bại", result.message, "OK");
            }
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }
    }
}
