using FoodMapApp.Services;
using System.Net.Http.Json;
using Microsoft.Maui.Storage;

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
            LoadProfileFromSession();
            await LoadHistory();
        }

        private void LoadProfileFromSession()
        {
            int userId = Preferences.Default.Get("user_id", 0);
            string username = Preferences.Default.Get("username", "Khach");
            string role = Preferences.Default.Get("role", "user");
            string email = Preferences.Default.Get("email", "");

            ProfileUsernameLabel.Text = username;
            ProfileEmailLabel.Text = email;
            ProfileRoleLabel.Text = role == "CNH" ? "Chu Nha Hang" : role;
        }

        private async Task LoadHistory()
        {
            int userId = Preferences.Default.Get("user_id", 0);
            if (userId == 0) return;

            string baseUrl = $"http://{BackendIp}:5000/api";

            try
            {
                var client = new HttpClient();

                // Load visit history
                var visits = await client.GetFromJsonAsync<List<VisitHistory>>($"{baseUrl}/food");
                if (visits != null)
                {
                    HistoryCollection.ItemsSource = visits.Take(5).ToList();
                    VisitCountLabel.Text = visits.Count.ToString();
                }

                // Load reviews from all POIs
                var allReviews = new List<ReviewHistory>();
                ReviewsCollection.ItemsSource = allReviews;
                ReviewCountLabel.Text = allReviews.Count.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading profile: {ex.Message}");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Dang xuat", "Ban co chac muon dang xuat?", "Co", "Huy");
            if (!confirm) return;

            var authService = new AuthService();
            authService.Logout();

            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    public class VisitHistory
    {
        public string name { get; set; }
        public string address { get; set; }
    }

    public class ReviewHistory
    {
        public string comment { get; set; }
        public string created_at { get; set; }
    }
}
