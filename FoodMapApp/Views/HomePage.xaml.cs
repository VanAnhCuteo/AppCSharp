using FoodMapApp.Services;
using System.Net.Http.Json;

namespace FoodMapApp.Views
{
    public partial class HomePage : ContentPage
    {
        private const string BackendIp = "10.0.2.2";
        private const string BackendUrl = $"http://{BackendIp}:5000/api/food";

        public HomePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Set greeting and username from session
            var username = AuthService.CurrentUsername;
            UsernameLabel.Text = string.IsNullOrEmpty(username) ? "Khach" : username;

            var hour = DateTime.Now.Hour;
            GreetingLabel.Text = hour < 12 ? "Chao buoi sang!" :
                                 hour < 18 ? "Chao buoi chieu!" : "Chao buoi toi!";

            await LoadRestaurants();
        }

        private async Task LoadRestaurants()
        {
            try
            {
                var client = new HttpClient();
                var foods = await client.GetFromJsonAsync<List<FoodItem>>($"{BackendUrl}?lang=vi");
                if (foods != null)
                    RestaurantsCollection.ItemsSource = foods;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading restaurants: {ex.Message}");
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Search filtering can be expanded here
        }
    }

    public class FoodItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string open_time { get; set; }
        public string image_url { get; set; }
    }
}
