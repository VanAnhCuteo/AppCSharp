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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Set Vietnamese text from code-behind to avoid XAML encoding issues
            TitleLabel.Text = "Tham Gia FoodTour";
            SubtitleLabel.Text = "Tạo tài khoản để khám phá ẩm thực Bùi Viện.";
            UsernameLbl.Text = "Tên đăng nhập";
            UsernameEntry.Placeholder = "Chọn tên đăng nhập";
            EmailLbl.Text = "Địa chỉ Email";
            PasswordLbl.Text = "Mật khẩu";
            ConfirmPasswordLbl.Text = "Xác nhận mật khẩu";
            RegisterButton.Text = "ĐĂNG KÝ";
            HaveAccountLabel.Text = "Đã có tài khoản?";
            LoginNowLabel.Text = "Đăng nhập";
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;
            string confirmPassword = ConfirmPasswordEntry.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Lỗi", "Vui lòng điền đầy đủ thông tin.", "OK");
                return;
            }

            if (password != confirmPassword)
            {
                await DisplayAlert("Lỗi", "Mật khẩu xác nhận không khớp.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            var result = await _authService.RegisterAsync(username, email, password);
            LoadingIndicator.IsRunning = false;

            if (result.success)
            {
                await DisplayAlert("Thành công", "Tài khoản đã tạo! Chào mừng đến FoodTour Bùi Viện.", "OK");
                await Shell.Current.GoToAsync("//HomePage");
            }
            else
            {
                await DisplayAlert("Đăng ký thất bại", result.message, "OK");
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
