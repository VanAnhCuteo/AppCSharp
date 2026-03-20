using FoodMapApp.Services;

namespace FoodMapApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _authService;

        public LoginPage()
        {
            InitializeComponent();
            _authService = new AuthService(); // In a real app, use Dependency Injection
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string identifier = IdentifierEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Please enter both username/email and password.", "OK");
                return;
            }

            LoadingIndicator.IsRunning = true;
            var result = await _authService.LoginAsync(identifier, password);
            LoadingIndicator.IsRunning = false;

            if (result.success)
            {
                // Navigate to food tour intro page
                await Shell.Current.GoToAsync("//FoodTourPage");
            }
            else
            {
                await DisplayAlert("Login Failed", result.message, "OK");
            }
        }

        private async void OnRegisterTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("RegisterPage");
        }
    }
}
