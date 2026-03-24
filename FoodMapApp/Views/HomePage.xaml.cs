using FoodMapApp.Services;
using System.Net.Http.Json;
using System.Linq;
using System.Text;
using System.Globalization;

namespace FoodMapApp.Views
{
    public partial class HomePage : ContentPage
    {
        private static string BackendUrl => AppConfig.FoodApiUrl;
        private List<FoodItem> _allRestaurants = new();
        private List<CategoryItem> _allCategories = new();
        private int? _selectedCategoryId = null;

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

            TopRestaurantLabel.Text = "Những Quán Bạn Cần";
            SeeAllLabel.Text = "";

            EventTitleLabel.Text = "Sự Kiện & Ưu Đãi";
            Event1Title.Text = "Happy Hour \U0001F37B";
            Event1Desc.Text = "Giảm 30% bia tươi từ 17:00 - 19:00";
            Event1Sub.Text = "Áp dụng toàn tuyến Bùi Viện";
            Event2Title.Text = "Combo Ăn Vặt \U0001F354";
            Event2Desc.Text = "Combo 2 người chỉ từ 150.000đ";
            Event2Sub.Text = "Nhiều quán tham gia";

            await LoadCategories();
            await LoadRestaurants();
        }

        private async Task LoadCategories()
        {
            try
            {
                var client = new HttpClient();
                var categories = await client.GetFromJsonAsync<List<CategoryItem>>($"{BackendUrl}/categories");
                if (categories != null)
                {
                    _allCategories = categories;
                    // Assign icons and colors
                    foreach (var cat in _allCategories)
                    {
                        if (cat.category_name.Contains("Hải sản")) { cat.Icon = "🦐"; cat.BackgroundColor = "#F0F8FF"; }
                        else if (cat.category_name.Contains("vặt")) { cat.Icon = "🍿"; cat.BackgroundColor = "#FFFBF0"; }
                        else if (cat.category_name.Contains("nướng")) { cat.Icon = "🍗"; cat.BackgroundColor = "#FFF0F3"; }
                        else { cat.Icon = "🍴"; cat.BackgroundColor = "#F0FFF4"; }
                    }
                    CategoriesCollection.ItemsSource = _allCategories;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private async Task LoadRestaurants(int? categoryId = null)
        {
            try
            {
                var client = new HttpClient();
                string url = $"{BackendUrl}?lang=vi";
                if (categoryId.HasValue) url += $"&category_id={categoryId.Value}";

                var foods = await client.GetFromJsonAsync<List<FoodItem>>(url);
                if (foods != null)
                {
                    _allRestaurants = foods;
                    RestaurantsCollection.ItemsSource = _allRestaurants;

                    // Update visibility if searching
                    if (!string.IsNullOrWhiteSpace(SearchEntry.Text))
                        OnSearchTextChanged(null, new TextChangedEventArgs(null, SearchEntry.Text));
                    else
                    {
                        RestaurantsCollection.IsVisible = _allRestaurants.Any();
                        NoResultsLabel.IsVisible = !_allRestaurants.Any();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading restaurants: {ex.Message}");
            }
        }

        private async void OnCategoryTapped(object sender, TappedEventArgs e)
        {
            var category = (CategoryItem)e.Parameter;
            if (category == null) return;

            // Simple toggle logic or just select
            if (_selectedCategoryId == category.category_id)
            {
                _selectedCategoryId = null; // Unselect
            }
            else
            {
                _selectedCategoryId = category.category_id;
            }

            await LoadRestaurants(_selectedCategoryId);
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";
            var searchNormalized = RemoveDiacritics(searchText);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                RestaurantsCollection.ItemsSource = _allRestaurants;
                RestaurantsCollection.IsVisible = true;
                NoResultsLabel.IsVisible = false;
                return;
            }

            var filtered = _allRestaurants.Where(f =>
            {
                var nameNormalized = RemoveDiacritics(f.name?.ToLower() ?? "");
                var descNormalized = RemoveDiacritics(f.description?.ToLower() ?? "");
                var addrNormalized = RemoveDiacritics(f.address?.ToLower() ?? "");

                return nameNormalized.Contains(searchNormalized) ||
                       descNormalized.Contains(searchNormalized) ||
                       addrNormalized.Contains(searchNormalized);
            }).ToList();

            if (filtered.Any())
            {
                RestaurantsCollection.ItemsSource = filtered;
                RestaurantsCollection.IsVisible = true;
                NoResultsLabel.IsVisible = false;
            }
            else
            {
                RestaurantsCollection.ItemsSource = null;
                RestaurantsCollection.IsVisible = false;
                NoResultsLabel.IsVisible = true;
            }
        }

        private void OnBackgroundTapped(object sender, EventArgs e)
        {
            SearchEntry.Unfocus();
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLower();
        }
    }

    public class FoodItem
    {
        public int id { get; set; }
        public int category_id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? address { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string? open_time { get; set; }
        public string? image_url { get; set; }
    }

    public class CategoryItem
    {
        public int category_id { get; set; }
        public string? category_name { get; set; }
        public string? description { get; set; }
        public string? Icon { get; set; }
        public string? BackgroundColor { get; set; }
    }
}
