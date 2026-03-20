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

            // Set Vietnamese text from code-behind
            var username = AuthService.CurrentUsername;
            UsernameLabel.Text = string.IsNullOrEmpty(username) ? "Khách" : username;

            var hour = DateTime.Now.Hour;
            GreetingLabel.Text = hour < 12 ? "Chào buổi sáng!" :
                                 hour < 18 ? "Chào buổi chiều!" : "Chào buổi tối!";

            SearchEntry.Placeholder = "Tìm món ăn, quán, BBQ, nhậu...";

            CategoryTitleLabel.Text = "Danh Mục";
            Cat1Label.Text = "Đồ Nướng";
            Cat2Label.Text = "Hải Sản";
            Cat3Label.Text = "Đồ Uống";
            Cat4Label.Text = "Ăn Đêm";

            TopRestaurantLabel.Text = "Top Quán Hot";
            SeeAllLabel.Text = "Xem tất cả >";

            EventTitleLabel.Text = "Sự Kiện & Ưu Đãi";
            Event1Title.Text = "Happy Hour \U0001F37B";
            Event1Desc.Text = "Giảm 30% bia tươi từ 17:00 - 19:00";
            Event1Sub.Text = "Áp dụng toàn tuyến Bùi Viện";
            Event2Title.Text = "Combo Ăn Vặt \U0001F354";
            Event2Desc.Text = "Combo 2 người chỉ từ 150.000đ";
            Event2Sub.Text = "Nhiều quán tham gia";

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
            // Search filtering
        }
    }

    public class FoodItem
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string? open_time { get; set; }
        public string? image_url { get; set; }
    }
}
